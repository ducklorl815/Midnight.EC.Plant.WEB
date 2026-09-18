using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.External;

namespace Midnight.EC.Plant.WEB.Services.AI;

public class CareKnowledgeSynthesisService
{
    public const string PromptVersion = "care-synthesis-v7";

    private readonly IChatCompletionEnvelope _chat;
    private readonly ILogger<CareKnowledgeSynthesisService> _logger;

    public CareKnowledgeSynthesisService(
        IChatCompletionEnvelope chat,
        ILogger<CareKnowledgeSynthesisService> logger)
    {
        _chat = chat;
        _logger = logger;
    }

    public async Task<CareSynthesisResult> SynthesizeAsync(
        string speciesKeyword,
        string? scientificName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userPrompt = BuildPrompt(speciesKeyword, scientificName);
            var content = await _chat.CompleteAsync(
                [
                    new ChatCompletionMessage
                    {
                        Role = "system",
                        Text = """
                            你是台灣居家植栽顧問。輸出物種照護知識 JSON（species-care-v3）。
                            欄位名英文；給使用者讀的字串用繁體中文（台灣用字），禁止簡體。

                            【核心】一個照護事實只能有一個主要模組。禁止在不同模組重寫相同內容。
                            禁止產出「養護重點總覽／照護摘要」這類重抄型模組。
                            「我的環境」不要產出（那是單盆適配，另條路徑）。
                            沒有可靠資料的模組請省略，不要硬湊。

                            輸出結構：
                            {
                              chineseName, scientificName,
                              quickFacts: {
                                light: None|Diffuse|HalfDay|FullSun,
                                wateringStrategy: 如 dry_then_soak,
                                wateringIntervalHint: 如 7-14d（僅參考）,
                                humidity: low|medium|high 或短繁中,
                                temperatureMin, temperatureMax（JSON 數字）,
                                growingSeason: ["spring","summer",...]
                              },
                              modules: [
                                { id, content, basics?, warnings?, tips? }
                              ],
                              fertilizerRecipe: { type, npkHint, dilution, dilutionStrong, dilutionMild, frequency, notes }
                            }

                            modules.id 只能用：
                            basic-info, identification, light, watering, humidity, temperature,
                            soil, fertilizer, growing-season, flowering, propagation, lookalikes

                            basic-info：只放 basics 表（englishName,family,subfamily,genus,plantType,height,stem,flower,growthHabit,nativeRange,alsoKnownAs）；content 可空；不要寫照護建議。
                            identification：只認識植物／外觀特徵；不要寫澆水光照。
                            light／watering／humidity／temperature／soil：各管各的，互不重抄。
                            watering：優先寫「乾透再澆」等原則；間隔只作參考。
                            fertilizer：施肥原則短文（是否需要、頻率、季節）；具體 NPK／稀釋只放 fertilizerRecipe，不要在 content 再抄一遍。
                            flowering：若需提光照，最多一句「充足光照有助開花」，不要重抄 light 模組。
                            chineseName 只能填使用者輸入的中文名；別名放 basics.alsoKnownAs。
                            """
                    },
                    new ChatCompletionMessage { Role = "user", Text = userPrompt }
                ],
                cancellationToken: cancellationToken);

            ExternalKnowledgePartial partial;
            try
            {
                partial = ParseCarePartial(content);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Care synthesis JSON parse failed. Content: {Content}", Truncate(content, 400));
                return CareSynthesisResult.Failed($"無法解析 OpenAI JSON：{Truncate(ex.Message, 160)}");
            }

            if (IsEffectivelyEmpty(partial))
            {
                return CareSynthesisResult.Failed("OpenAI 有回應，但未提供可用的照護欄位。");
            }

            return CareSynthesisResult.Succeeded(partial);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Care knowledge synthesis failed for {Keyword}", speciesKeyword);
            return CareSynthesisResult.Failed($"呼叫 OpenAI 例外：{ex.GetType().Name} — {Truncate(ex.Message, 180)}");
        }
    }

    private ExternalKnowledgePartial ParseCarePartial(string content)
    {
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // v3：根層 quickFacts + modules
        SpeciesCareGuideDto? guide = null;
        if (TryGetPropertyIgnoreCase(root, "modules", out _)
            || TryGetPropertyIgnoreCase(root, "quickFacts", out _))
        {
            guide = ParseV3Guide(root);
        }
        else if (TryGetPropertyIgnoreCase(root, "speciesGuide", out var guideEl) &&
                 guideEl.ValueKind == JsonValueKind.Object)
        {
            // 相容舊 speciesGuide 包一層
            if (TryGetPropertyIgnoreCase(guideEl, "modules", out _)
                || TryGetPropertyIgnoreCase(guideEl, "quickFacts", out _))
            {
                guide = ParseV3Guide(guideEl);
            }
            else
            {
                guide = ParseLegacySpeciesGuideObject(guideEl);
            }
        }

        if (guide == null || !guide.HasArticleContent())
        {
            var legacy = NullIfEmpty(GetString(root, "externalCareGuide"));
            if (!string.IsNullOrWhiteSpace(legacy) && !CareGuideJson.LooksLikeJson(legacy))
            {
                guide = new SpeciesCareGuideDto
                {
                    Modules =
                    [
                        new CareKnowledgeModuleDto
                        {
                            Id = CareGuideModuleIds.Identification,
                            Content = legacy
                        }
                    ]
                };
            }
        }

        if (guide == null)
        {
            return new ExternalKnowledgePartial { Provider = $"AI-{PromptVersion}" };
        }

        CareGuideJson.NormalizeToV3(guide);
        var stored = CareGuideJson.ForStorage(guide);
        var fertilizerRequirement = NullIfEmpty(CareGuideJson.FormatFertilizerRequirement(stored.FertilizerRecipe));

        // 由 quickFacts 投影短欄（不再接受 AI 另寫一套散文短欄為權威）
        var projected = new ExternalKnowledgePartial
        {
            ExternalCareGuide = CareGuideJson.Serialize(stored),
            FertilizerRequirement = fertilizerRequirement,
            CareSummary = NullIfEmpty(stored.GetWallIntro()),
            Provider = $"AI-{PromptVersion}"
        };

        ApplyQuickFactsToPartial(projected, stored.QuickFacts);

        // 相容：若 AI 仍給舊根欄且 quickFacts 缺，才回填
        projected.SuggestedLight ??= ParseSuggestedLight(root);
        projected.LightRequirement ??= NullIfEmpty(GetString(root, "lightRequirement"));
        projected.WaterRequirement ??= NullIfEmpty(GetString(root, "waterRequirement"));
        projected.HumidityRequirement ??= NullIfEmpty(GetString(root, "humidityRequirement"));
        projected.TemperatureMin ??= GetOptionalDecimal(root, "temperatureMin");
        projected.TemperatureMax ??= GetOptionalDecimal(root, "temperatureMax");
        projected.SoilRequirement ??= NullIfEmpty(GetString(root, "soilRequirement"));
        projected.GrowthSeason ??= NullIfEmpty(GetString(root, "growthSeason"));
        if (string.IsNullOrWhiteSpace(projected.FertilizerRequirement))
        {
            projected.FertilizerRequirement = NullIfEmpty(GetString(root, "fertilizerRequirement"));
        }

        return projected;
    }

    private static void ApplyQuickFactsToPartial(ExternalKnowledgePartial partial, CareQuickFactsDto? facts)
    {
        if (facts == null || !facts.HasAny()) return;

        if (facts.Light.HasValue)
        {
            partial.SuggestedLight = facts.Light;
            partial.LightRequirement = Midnight.EC.Plant.WEB.Models.Enums.LightLevelDisplay.ToLabel(facts.Light);
        }

        if (!string.IsNullOrWhiteSpace(facts.WateringStrategy) || !string.IsNullOrWhiteSpace(facts.WateringIntervalHint))
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(facts.WateringStrategy))
            {
                var s = facts.WateringStrategy.Trim().ToLowerInvariant();
                parts.Add(s is "dry_then_soak" or "dry-then-soak" or "dry_then_water"
                    ? "介質乾透後再澆透"
                    : facts.WateringStrategy.Trim());
            }

            if (!string.IsNullOrWhiteSpace(facts.WateringIntervalHint))
            {
                parts.Add($"參考間隔 {facts.WateringIntervalHint.Trim()}");
            }

            partial.WaterRequirement = string.Join("；", parts);
        }

        if (!string.IsNullOrWhiteSpace(facts.Humidity))
        {
            var h = facts.Humidity.Trim().ToLowerInvariant();
            partial.HumidityRequirement = h switch
            {
                "low" => "低濕",
                "medium" or "moderate" => "中濕",
                "high" => "高濕",
                _ => facts.Humidity.Trim()
            };
        }

        partial.TemperatureMin = facts.TemperatureMin ?? partial.TemperatureMin;
        partial.TemperatureMax = facts.TemperatureMax ?? partial.TemperatureMax;

        if (facts.GrowingSeason.Count > 0)
        {
            partial.GrowthSeason = string.Join("、", facts.GrowingSeason.Select(s =>
            {
                var t = s.Trim().ToLowerInvariant();
                return t switch
                {
                    "spring" => "春",
                    "summer" => "夏",
                    "autumn" or "fall" => "秋",
                    "winter" => "冬",
                    _ => s.Trim()
                };
            }));
        }
    }

    private SpeciesCareGuideDto ParseV3Guide(JsonElement root)
    {
        var guide = new SpeciesCareGuideDto
        {
            ChineseName = NullIfEmpty(GetString(root, "chineseName")),
            ScientificName = NullIfEmpty(GetString(root, "scientificName")),
            QuickFacts = ParseQuickFacts(root),
            FertilizerRecipe = ParseFertilizer(root) ?? ParseFertilizerNamed(root, "fertilizerRecipe")
        };

        if (TryGetPropertyIgnoreCase(root, "modules", out var modulesEl) && modulesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in modulesEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var id = NullIfEmpty(GetString(item, "id"));
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (string.Equals(id, "carePriority", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(id, "care-summary", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(id, "environment-analysis", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var mapped = CareGuideSectionOrderDto.MapLegacySectionKey(id) ?? id;
                if (!CareGuideModuleIds.SpeciesContentIds.Contains(mapped, StringComparer.OrdinalIgnoreCase)
                    && !string.Equals(mapped, CareGuideModuleIds.Lookalikes, StringComparison.OrdinalIgnoreCase))
                {
                    // 仍允許登錄內物種模組
                    if (CareGuideModuleRegistry.Find(mapped) is not { IsPersonal: false, IsShared: false })
                    {
                        continue;
                    }
                }

                var mod = new CareKnowledgeModuleDto
                {
                    Id = mapped,
                    Content = NullIfEmpty(GetString(item, "content")),
                    Basics = ParseBasicsObject(item),
                    Warnings = ParseStringArray(item, "warnings"),
                    Tips = ParseStringArray(item, "tips")
                };

                // basics 也可能掛在 basic-info 的 basics 子物件，或根層 basics
                if (string.Equals(mapped, CareGuideModuleIds.BasicInfo, StringComparison.OrdinalIgnoreCase)
                    && mod.Basics == null)
                {
                    mod.Basics = ParseBasics(root) ?? ParseBasicsObject(item);
                }

                if (mod.HasContent() || string.Equals(mapped, CareGuideModuleIds.Fertilizer, StringComparison.OrdinalIgnoreCase))
                {
                    guide.Modules.Add(mod);
                }
            }
        }

        return guide;
    }

    private SpeciesCareGuideDto ParseLegacySpeciesGuideObject(JsonElement guideEl)
    {
        return new SpeciesCareGuideDto
        {
            ChineseName = NullIfEmpty(GetString(guideEl, "chineseName")),
            ScientificName = NullIfEmpty(GetString(guideEl, "scientificName")),
            Summary = NullIfEmpty(GetString(guideEl, "summary")),
            Basics = ParseBasics(guideEl),
            Identification = NullIfEmpty(GetString(guideEl, "identification")),
            CarePriority = NullIfEmpty(GetString(guideEl, "carePriority")),
            Light = NullIfEmpty(GetString(guideEl, "light")),
            Watering = NullIfEmpty(GetString(guideEl, "watering")),
            Substrate = NullIfEmpty(GetString(guideEl, "substrate")),
            Temperature = NullIfEmpty(GetString(guideEl, "temperature")),
            Propagation = NullIfEmpty(GetString(guideEl, "propagation")),
            Flowering = NullIfEmpty(GetString(guideEl, "flowering")),
            Lookalikes = NullIfEmpty(GetString(guideEl, "lookalikes")),
            Fertilizer = ParseFertilizer(guideEl),
            FertilizerRecipe = ParseFertilizer(guideEl)
        };
    }

    private static CareQuickFactsDto? ParseQuickFacts(JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "quickFacts", out var el) || el.ValueKind != JsonValueKind.Object)
        {
            // 相容：根層 suggestedLight / temperature*
            var light = ParseSuggestedLight(root);
            var tMin = GetOptionalDecimal(root, "temperatureMin");
            var tMax = GetOptionalDecimal(root, "temperatureMax");
            if (light == null && tMin == null && tMax == null) return null;
            return new CareQuickFactsDto
            {
                Light = light,
                TemperatureMin = tMin,
                TemperatureMax = tMax
            };
        }

        var facts = new CareQuickFactsDto
        {
            Light = ParseSuggestedLightFromElement(el),
            WateringStrategy = NullIfEmpty(GetString(el, "wateringStrategy")),
            WateringIntervalHint = NullIfEmpty(GetString(el, "wateringIntervalHint")),
            Humidity = NullIfEmpty(GetString(el, "humidity")),
            TemperatureMin = GetOptionalDecimal(el, "temperatureMin"),
            TemperatureMax = GetOptionalDecimal(el, "temperatureMax"),
            GrowingSeason = ParseStringArray(el, "growingSeason")
        };

        return facts.HasAny() ? facts : null;
    }

    private static Midnight.EC.Plant.WEB.Models.Enums.LightLevel? ParseSuggestedLightFromElement(JsonElement parent)
    {
        if (!TryGetPropertyIgnoreCase(parent, "light", out var el)
            && !TryGetPropertyIgnoreCase(parent, "suggestedLight", out el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.String => Midnight.EC.Plant.WEB.Models.Enums.LightLevelDisplay.TryParseSuggestedLightToken(el.GetString()),
            JsonValueKind.Number when el.TryGetInt32(out var n)
                => Midnight.EC.Plant.WEB.Models.Enums.LightLevelDisplay.TryParseSuggestedLightToken(n.ToString()),
            _ => Midnight.EC.Plant.WEB.Models.Enums.LightLevelDisplay.TryParseSuggestedLightToken(el.ToString())
        };
    }

    private static FertilizerRecipeDto? ParseFertilizerNamed(JsonElement parent, string name)
    {
        if (!TryGetPropertyIgnoreCase(parent, name, out var el) || el.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ParseFertilizerObject(el);
    }

    private static CareBasicsDto? ParseBasicsObject(JsonElement parent)
    {
        if (!TryGetPropertyIgnoreCase(parent, "basics", out var el) || el.ValueKind != JsonValueKind.Object)
        {
            // 若 parent 本身就是 basics 欄位集合
            if (parent.ValueKind != JsonValueKind.Object) return null;
            el = parent;
            if (!TryGetPropertyIgnoreCase(el, "englishName", out _)
                && !TryGetPropertyIgnoreCase(el, "family", out _)
                && !TryGetPropertyIgnoreCase(el, "plantType", out _))
            {
                return null;
            }
        }

        var basics = new CareBasicsDto
        {
            EnglishName = NullIfEmpty(GetString(el, "englishName")),
            Family = NullIfEmpty(GetString(el, "family")),
            Subfamily = NullIfEmpty(GetString(el, "subfamily")),
            Genus = NullIfEmpty(GetString(el, "genus")),
            PlantType = NullIfEmpty(GetString(el, "plantType")),
            Height = NullIfEmpty(GetString(el, "height")),
            Stem = NullIfEmpty(GetString(el, "stem")),
            Flower = NullIfEmpty(GetString(el, "flower")),
            GrowthHabit = NullIfEmpty(GetString(el, "growthHabit")),
            NativeRange = NullIfEmpty(GetString(el, "nativeRange")),
            AlsoKnownAs = ParseStringArray(el, "alsoKnownAs")
        };

        return SpeciesCareGuideDto.BasicsHasContent(basics) ? basics : null;
    }

    private static Midnight.EC.Plant.WEB.Models.Enums.LightLevel? ParseSuggestedLight(JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "suggestedLight", out var el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.String => Midnight.EC.Plant.WEB.Models.Enums.LightLevelDisplay.TryParseSuggestedLightToken(el.GetString()),
            JsonValueKind.Number when el.TryGetInt32(out var n)
                => Midnight.EC.Plant.WEB.Models.Enums.LightLevelDisplay.TryParseSuggestedLightToken(n.ToString()),
            _ => Midnight.EC.Plant.WEB.Models.Enums.LightLevelDisplay.TryParseSuggestedLightToken(el.ToString())
        };
    }

    private static CareBasicsDto? ParseBasics(JsonElement parent)
    {
        if (!TryGetPropertyIgnoreCase(parent, "basics", out var el) || el.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var basics = new CareBasicsDto
        {
            EnglishName = NullIfEmpty(GetString(el, "englishName")),
            Family = NullIfEmpty(GetString(el, "family")),
            Subfamily = NullIfEmpty(GetString(el, "subfamily")),
            PlantType = NullIfEmpty(GetString(el, "plantType")),
            Height = NullIfEmpty(GetString(el, "height")),
            Stem = NullIfEmpty(GetString(el, "stem")),
            Flower = NullIfEmpty(GetString(el, "flower")),
            GrowthHabit = NullIfEmpty(GetString(el, "growthHabit")),
            AlsoKnownAs = ParseStringArray(el, "alsoKnownAs")
        };

        if (string.IsNullOrWhiteSpace(basics.EnglishName)
            && string.IsNullOrWhiteSpace(basics.Family)
            && string.IsNullOrWhiteSpace(basics.PlantType)
            && basics.AlsoKnownAs.Count == 0)
        {
            return null;
        }

        return basics;
    }

    private static FertilizerRecipeDto? ParseFertilizer(JsonElement parent)
    {
        if (!TryGetPropertyIgnoreCase(parent, "fertilizer", out var el) || el.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ParseFertilizerObject(el);
    }

    private static FertilizerRecipeDto? ParseFertilizerObject(JsonElement el)
    {
        var recipe = new FertilizerRecipeDto
        {
            Type = NullIfEmpty(GetString(el, "type")),
            NpkHint = NullIfEmpty(GetString(el, "npkHint")),
            Dilution = NullIfEmpty(GetString(el, "dilution")),
            DilutionStrong = GetOptionalInt(el, "dilutionStrong"),
            DilutionMild = GetOptionalInt(el, "dilutionMild"),
            Frequency = NullIfEmpty(GetString(el, "frequency")),
            Notes = NullIfEmpty(GetString(el, "notes"))
        };

        if (recipe.DilutionStrong is null || recipe.DilutionMild is null)
        {
            var (strong, mild) = CareGuideJson.ResolveDilutionRange(recipe);
            recipe.DilutionStrong ??= strong;
            recipe.DilutionMild ??= mild;
            if (string.IsNullOrWhiteSpace(recipe.Dilution))
            {
                recipe.Dilution = $"{recipe.DilutionStrong}–{recipe.DilutionMild} 倍";
            }
        }

        if (string.IsNullOrWhiteSpace(recipe.Type)
            && string.IsNullOrWhiteSpace(recipe.Dilution)
            && string.IsNullOrWhiteSpace(recipe.Frequency))
        {
            return null;
        }

        return recipe;
    }

    private static List<string> ParseStringArray(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return el.EnumerateArray()
            .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToList();
    }

    private static string? GetString(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => el.ToString()
        };
    }

    private static decimal? GetOptionalDecimal(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Number when el.TryGetDecimal(out var n) => n,
            JsonValueKind.String => ParseDecimalLoose(el.GetString()),
            _ => ParseDecimalLoose(el.ToString())
        };
    }

    private static int? GetOptionalInt(JsonElement root, string name)
    {
        var d = GetOptionalDecimal(root, name);
        if (!d.HasValue)
        {
            return null;
        }

        return (int)Math.Round(d.Value, MidpointRounding.AwayFromZero);
    }

    private static decimal? ParseDecimalLoose(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim();
        if (text is "null" or "undefined" or "-" or "未知" or "N/A" or "n/a")
        {
            return null;
        }

        // 抽出第一個數字（允許負號與小數），例如 "10°C"、"約 18 度"、"10-28"
        var match = System.Text.RegularExpressions.Regex.Match(text, @"-?\d+(\.\d+)?");
        if (!match.Success)
        {
            return null;
        }

        return decimal.TryParse(match.Value, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string name, out JsonElement value)
    {
        if (root.TryGetProperty(name, out value))
        {
            return true;
        }

        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool IsEffectivelyEmpty(ExternalKnowledgePartial p) =>
        string.IsNullOrWhiteSpace(p.LightRequirement)
        && string.IsNullOrWhiteSpace(p.WaterRequirement)
        && string.IsNullOrWhiteSpace(p.HumidityRequirement)
        && !p.TemperatureMin.HasValue
        && !p.TemperatureMax.HasValue
        && string.IsNullOrWhiteSpace(p.SoilRequirement)
        && string.IsNullOrWhiteSpace(p.FertilizerRequirement)
        && string.IsNullOrWhiteSpace(p.GrowthSeason)
        && string.IsNullOrWhiteSpace(p.ExternalCareGuide);

    private static string BuildPrompt(string keyword, string? scientificName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("請以繁體中文（台灣用字）產出 species-care-v3；不要使用簡體中文。");
        sb.AppendLine($"使用者輸入（中文名以此為準，禁止改成其他俗名）：{keyword}");
        sb.AppendLine($"學名：{scientificName ?? "未知"}");
        sb.AppendLine("請強制重寫為模組化結果，不要依賴外部資料庫摘要。");
        sb.AppendLine("規則：一事實一模組；不要寫養護重點總覽；不要產出 environment-analysis。");
        sb.AppendLine("quickFacts.light 必填：None / Diffuse / HalfDay / FullSun。");
        sb.AppendLine("temperatureMin / temperatureMax 必須是純數字。");
        sb.AppendLine("wateringStrategy 優先 dry_then_soak；wateringIntervalHint 僅參考。");
        return sb.ToString();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Services.Configuration;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.AI;

public class CareKnowledgeSynthesisService : ICareKnowledgeSynthesisService
{
    public const string PromptVersion = "care-synthesis-v7";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiOptions _options;
    private readonly ILogger<CareKnowledgeSynthesisService> _logger;

    public CareKnowledgeSynthesisService(
        IHttpClientFactory httpClientFactory,
        IOptions<AiOptions> options,
        ILogger<CareKnowledgeSynthesisService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CareSynthesisResult> SynthesizeAsync(
        string speciesKeyword,
        string? scientificName,
        ExternalKnowledgeResult mergedKnowledge,
        CancellationToken cancellationToken = default,
        bool forceRefreshGuide = false)
    {
        if (!forceRefreshGuide && !CareKnowledgeCompleteness.HasGaps(mergedKnowledge))
        {
            return CareSynthesisResult.Skipped();
        }

        if (string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey))
        {
            _logger.LogWarning("OpenAI key not configured; AI 補足 skipped.");
            return CareSynthesisResult.Failed("尚未設定 AI:OpenAI:ApiKey（請確認 Development 設定或 User Secrets）。");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("OpenAI");
            var userPrompt = BuildPrompt(speciesKeyword, scientificName, mergedKnowledge);
            var payload = new
            {
                model = _options.OpenAI.Model,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = """
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
                    new { role = "user", content = userPrompt }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAI.ApiKey.Trim());
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var detail = Truncate(ExtractOpenAiError(responseBody) ?? responseBody, 240);
                _logger.LogWarning(
                    "Care synthesis OpenAI failed: {Status} {Detail}",
                    (int)response.StatusCode,
                    detail);
                return CareSynthesisResult.Failed($"OpenAI HTTP {(int)response.StatusCode}：{detail}");
            }

            using var document = JsonDocument.Parse(responseBody);
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            content = StripMarkdownFence(content);
            if (string.IsNullOrWhiteSpace(content))
            {
                return CareSynthesisResult.Failed("OpenAI 回傳內容為空。");
            }

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

    public async Task<EnvironmentFitResult> SynthesizeEnvironmentFitAsync(
        string plantDisplayName,
        string? scientificName,
        ExternalKnowledgeResult knowledge,
        PlantEnvironmentContext environment,
        CancellationToken cancellationToken = default)
    {
        var hasEnv = !string.IsNullOrWhiteSpace(environment.PlacementLabel)
            || !string.IsNullOrWhiteSpace(environment.LightLabel)
            || environment.RainCoverLabel != null
            || !string.IsNullOrWhiteSpace(environment.SubstrateType)
            || !string.IsNullOrWhiteSpace(environment.SaucerLabel);

        if (!hasEnv)
        {
            return EnvironmentFitResult.Skipped();
        }

        if (string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey))
        {
            return EnvironmentFitResult.Failed("尚未設定 AI:OpenAI:ApiKey。");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("OpenAI");
            var userPrompt = BuildEnvironmentFitPrompt(plantDisplayName, scientificName, knowledge, environment);
            var payload = new
            {
                model = _options.OpenAI.Model,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = """
                            你是台灣居家植栽顧問。必須依「使用者實際環境」對照「物種照護需求」做適配分析。
                            【語言】全文繁體中文（台灣用字），禁止簡體。JSON 鍵名用英文。
                            【禁止】重抄物種光照／澆水／介質百科；只寫「物種需求 vs 你的環境」的差異、風險與調整。
                            若提供水盤狀態，必須在 adjustments 或 problems 中明確建議：拿掉水盤、可留盤但勿積水、或適合淺盤保濕。
                            輸出 JSON：
                            {
                              "fitSummary": "1～2 句契合／落差",
                              "problems": [ { "text": "問題與原因", "hint": "可選附註" } ],
                              "adjustments": [ "可執行調整1" ],
                              "fertilizerAdjustment": "一句話環境下施肥調整；不要重複完整配方教學"
                            }
                            problems 列 2～4 項。
                            """
                    },
                    new { role = "user", content = userPrompt }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAI.ApiKey.Trim());
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var detail = Truncate(ExtractOpenAiError(responseBody) ?? responseBody, 240);
                return EnvironmentFitResult.Failed($"OpenAI HTTP {(int)response.StatusCode}：{detail}");
            }

            using var document = JsonDocument.Parse(responseBody);
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            content = StripMarkdownFence(content);
            if (string.IsNullOrWhiteSpace(content))
            {
                return EnvironmentFitResult.Failed("OpenAI 回傳內容為空。");
            }

            using var adviceDoc = JsonDocument.Parse(content);
            var root = adviceDoc.RootElement;

            // 新結構
            if (TryGetPropertyIgnoreCase(root, "fitSummary", out _)
                || TryGetPropertyIgnoreCase(root, "problems", out _)
                || TryGetPropertyIgnoreCase(root, "adjustments", out _))
            {
                var dto = new EnvironmentAdviceDto
                {
                    FitSummary = NullIfEmpty(GetString(root, "fitSummary")),
                    FertilizerAdjustment = NullIfEmpty(GetString(root, "fertilizerAdjustment")),
                    Problems = ParseProblems(root),
                    Adjustments = ParseStringArray(root, "adjustments")
                };
                return EnvironmentFitResult.Ok(CareGuideJson.Serialize(dto));
            }

            // 舊版相容：advice 字串
            var advice = GetString(root, "advice");
            if (string.IsNullOrWhiteSpace(advice))
            {
                advice = content.Trim();
            }

            return EnvironmentFitResult.Ok(CareGuideJson.Serialize(new EnvironmentAdviceDto
            {
                FitSummary = advice.Trim()
            }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Environment fit synthesis failed for {Plant}", plantDisplayName);
            return EnvironmentFitResult.Failed($"呼叫 OpenAI 例外：{ex.GetType().Name} — {Truncate(ex.Message, 180)}");
        }
    }

    private static List<EnvironmentProblemDto> ParseProblems(JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "problems", out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<EnvironmentProblemDto>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var text = item.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    list.Add(new EnvironmentProblemDto { Text = text.Trim() });
                }
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var textVal = NullIfEmpty(GetString(item, "text")) ?? NullIfEmpty(GetString(item, "problem"));
            if (string.IsNullOrWhiteSpace(textVal))
            {
                continue;
            }

            list.Add(new EnvironmentProblemDto
            {
                Text = textVal,
                Hint = NullIfEmpty(GetString(item, "hint"))
            });
        }

        return list;
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

    private static string BuildEnvironmentFitPrompt(
        string plantDisplayName,
        string? scientificName,
        ExternalKnowledgeResult knowledge,
        PlantEnvironmentContext environment)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"植栽：{plantDisplayName}");
        sb.AppendLine($"學名：{scientificName ?? "未知"}");
        sb.AppendLine("【使用者實際環境】（必須以此為分析主軸）");
        sb.AppendLine($"- 實際位置類型：{environment.PlacementLabel ?? "未設定"}");
        sb.AppendLine($"- 實際日照：{environment.LightLabel ?? "未設定"}");
        sb.AppendLine($"- 遮雨：{environment.RainCoverLabel ?? "未設定"}");
        sb.AppendLine($"- 介質：{environment.SubstrateType ?? "未設定"}");
        sb.AppendLine($"- 水盤狀態：{environment.SaucerLabel ?? "未設定"}");
        sb.AppendLine($"- 縣市：{environment.City ?? "未設定"}");
        if (environment.MismatchWarnings.Count > 0)
        {
            sb.AppendLine("系統已偵測到的落差警告：");
            foreach (var w in environment.MismatchWarnings)
            {
                sb.AppendLine($"- {w}");
            }
        }

        sb.AppendLine("【物種照護需求（外部知識）】");
        sb.AppendLine($"- 光照：{knowledge.LightRequirement}");
        sb.AppendLine($"- 澆水：{knowledge.WaterRequirement}");
        sb.AppendLine($"- 濕度：{knowledge.HumidityRequirement}");
        sb.AppendLine($"- 溫度：{knowledge.TemperatureMin}~{knowledge.TemperatureMax}°C");
        sb.AppendLine($"- 土壤：{knowledge.SoilRequirement}");
        sb.AppendLine($"- 施肥：{knowledge.FertilizerRequirement}");
        sb.AppendLine($"- 生長季：{knowledge.GrowthSeason}");
        if (!string.IsNullOrWhiteSpace(knowledge.ExternalCareGuide))
        {
            var parsed = CareGuideJson.TryParseSpeciesGuide(knowledge.ExternalCareGuide);
            if (parsed != null)
            {
                sb.AppendLine($"- 物種說明：{parsed.Summary}");
                if (parsed.Fertilizer != null)
                {
                    sb.AppendLine($"- 物種施肥配方：{CareGuideJson.FormatFertilizerRequirement(parsed.Fertilizer)}");
                }
            }
            else
            {
                var guide = knowledge.ExternalCareGuide.Length > 600
                    ? knowledge.ExternalCareGuide[..600] + "…"
                    : knowledge.ExternalCareGuide;
                sb.AppendLine($"- 物種說明摘要：{guide}");
            }
        }

        sb.AppendLine("請產出結構化適配分析（fitSummary／problems／adjustments／fertilizerAdjustment），繁體中文。");
        return sb.ToString();
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

    private static string BuildPrompt(string keyword, string? scientificName, ExternalKnowledgeResult merged)
    {
        var sb = new StringBuilder();
        sb.AppendLine("請以繁體中文（台灣用字）產出 species-care-v3；不要使用簡體中文。");
        sb.AppendLine($"使用者輸入（中文名以此為準，禁止改成其他俗名）：{keyword}");
        sb.AppendLine($"學名：{scientificName ?? "未知"}");
        sb.AppendLine("參考資料（可忽略過時內容，請強制重寫為模組化結果）：");
        sb.AppendLine($"- 光照：{merged.LightRequirement}");
        sb.AppendLine($"- 澆水：{merged.WaterRequirement}");
        sb.AppendLine($"- 濕度：{merged.HumidityRequirement}");
        sb.AppendLine($"- 溫度：{merged.TemperatureMin}~{merged.TemperatureMax}°C");
        sb.AppendLine($"- 介質：{merged.SoilRequirement}");
        sb.AppendLine($"- 施肥：{merged.FertilizerRequirement}");
        sb.AppendLine($"- 生長季：{merged.GrowthSeason}");
        sb.AppendLine("規則：一事實一模組；不要寫養護重點總覽；不要產出 environment-analysis。");
        sb.AppendLine("quickFacts.light 必填：None / Diffuse / HalfDay / FullSun。");
        sb.AppendLine("temperatureMin / temperatureMax 必須是純數字。");
        sb.AppendLine("wateringStrategy 優先 dry_then_soak；wateringIntervalHint 僅參考。");
        return sb.ToString();
    }

    private static string? StripMarkdownFence(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return content;
        }

        var trimmed = content.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var lines = trimmed.Split('\n');
        if (lines.Length < 3)
        {
            return trimmed;
        }

        return string.Join('\n', lines.Skip(1).TakeWhile(l => !l.TrimStart().StartsWith("```", StringComparison.Ordinal))).Trim();
    }

    private static string? ExtractOpenAiError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                {
                    return message.GetString();
                }

                return error.ToString();
            }
        }
        catch
        {
            // ignore parse errors
        }

        return null;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

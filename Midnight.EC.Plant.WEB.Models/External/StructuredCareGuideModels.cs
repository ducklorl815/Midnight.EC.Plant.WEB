using System.Text.Json;
using System.Text.Json.Serialization;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Utility.Json;

namespace Midnight.EC.Plant.WEB.Models.External;

/// <summary>物種照護說明（結構化，存於 PlantKnowledge.ExternalCareGuide）。</summary>
public class SpeciesCareGuideDto
{
    public const string SchemaVersion = "species-care-v3";
    public const string LegacySchemaVersion = "species-guide-v2";

    public string Schema { get; set; } = SchemaVersion;
    public string? ChineseName { get; set; }
    public string? ScientificName { get; set; }

    /// <summary>快速照護資訊（權威結構化摘要）。</summary>
    public CareQuickFactsDto? QuickFacts { get; set; }

    /// <summary>物種知識模組（一事實一歸屬）。</summary>
    public List<CareKnowledgeModuleDto> Modules { get; set; } = [];

    /// <summary>施肥配方結構化欄（固定 UI 模板用）。</summary>
    public FertilizerRecipeDto? FertilizerRecipe { get; set; }

    // —— 以下為 v2 相容欄位（讀取後會升成 Modules）——
    public string? Summary { get; set; }
    public CareBasicsDto? Basics { get; set; }
    public string? Identification { get; set; }
    public string? CarePriority { get; set; }
    public string? Light { get; set; }
    public string? Watering { get; set; }
    public string? Substrate { get; set; }
    public string? Temperature { get; set; }
    public string? Propagation { get; set; }
    public string? Flowering { get; set; }
    public string? Lookalikes { get; set; }
    public FertilizerRecipeDto? Fertilizer { get; set; }

    public CareKnowledgeModuleDto? FindModule(string id) =>
        Modules.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));

    public bool HasArticleContent() =>
        Modules.Any(m => m.HasContent())
        || FertilizerRecipe != null
        || Fertilizer != null
        || BasicsHasContent(Basics)
        || !string.IsNullOrWhiteSpace(Identification)
        || !string.IsNullOrWhiteSpace(Light)
        || !string.IsNullOrWhiteSpace(Watering)
        || !string.IsNullOrWhiteSpace(Substrate)
        || !string.IsNullOrWhiteSpace(Summary);

    public string? GetWallIntro()
    {
        var id = FindModule(CareGuideModuleIds.Identification)?.Content;
        if (!string.IsNullOrWhiteSpace(id)) return id.Trim();
        if (!string.IsNullOrWhiteSpace(Identification)) return Identification.Trim();
        if (!string.IsNullOrWhiteSpace(Summary)) return Summary.Trim();
        return null;
    }

    public static bool BasicsHasContent(CareBasicsDto? b) =>
        b != null && (
            !string.IsNullOrWhiteSpace(b.EnglishName)
            || !string.IsNullOrWhiteSpace(b.Family)
            || !string.IsNullOrWhiteSpace(b.Subfamily)
            || !string.IsNullOrWhiteSpace(b.PlantType)
            || !string.IsNullOrWhiteSpace(b.Height)
            || !string.IsNullOrWhiteSpace(b.Stem)
            || !string.IsNullOrWhiteSpace(b.Flower)
            || !string.IsNullOrWhiteSpace(b.GrowthHabit)
            || (b.AlsoKnownAs?.Count > 0));
}

public class CareQuickFactsDto
{
    /// <summary>None / Diffuse / HalfDay / FullSun</summary>
    public LightLevel? Light { get; set; }

    /// <summary>例如 dry_then_soak</summary>
    public string? WateringStrategy { get; set; }

    /// <summary>例如 7-14d；僅參考</summary>
    public string? WateringIntervalHint { get; set; }

    /// <summary>low / medium / high 或短繁中</summary>
    public string? Humidity { get; set; }

    public decimal? TemperatureMin { get; set; }
    public decimal? TemperatureMax { get; set; }

    public List<string> GrowingSeason { get; set; } = [];

    public bool HasAny() =>
        Light.HasValue
        || !string.IsNullOrWhiteSpace(WateringStrategy)
        || !string.IsNullOrWhiteSpace(WateringIntervalHint)
        || !string.IsNullOrWhiteSpace(Humidity)
        || TemperatureMin.HasValue
        || TemperatureMax.HasValue
        || GrowingSeason.Count > 0;
}

public class CareKnowledgeModuleDto
{
    public string Id { get; set; } = string.Empty;
    public string? Content { get; set; }
    public CareBasicsDto? Basics { get; set; }
    public List<string> Warnings { get; set; } = [];
    public List<string> Tips { get; set; } = [];

    public bool HasContent() =>
        !string.IsNullOrWhiteSpace(Content)
        || SpeciesCareGuideDto.BasicsHasContent(Basics)
        || Warnings.Count > 0
        || Tips.Count > 0;
}

public class CareBasicsDto
{
    public string? EnglishName { get; set; }
    public string? Family { get; set; }
    public string? Subfamily { get; set; }
    public string? Genus { get; set; }
    public string? PlantType { get; set; }
    public string? Height { get; set; }
    public string? Stem { get; set; }
    public string? Flower { get; set; }
    public string? GrowthHabit { get; set; }
    public string? NativeRange { get; set; }
    public List<string> AlsoKnownAs { get; set; } = [];
}

public class FertilizerRecipeDto
{
    public string? Type { get; set; }
    public string? NpkHint { get; set; }
    public string? Dilution { get; set; }
    public int? DilutionStrong { get; set; }
    public int? DilutionMild { get; set; }
    public string? Frequency { get; set; }
    public string? Notes { get; set; }
}

public class EnvironmentAdviceDto
{
    public const string SchemaVersion = "env-advice-v1";

    public string Schema { get; set; } = SchemaVersion;
    public string? FitSummary { get; set; }
    public List<EnvironmentProblemDto> Problems { get; set; } = [];
    public List<string> Adjustments { get; set; } = [];
    public string? FertilizerAdjustment { get; set; }
}

public class EnvironmentProblemDto
{
    public string Text { get; set; } = string.Empty;
    public string? Hint { get; set; }
}

public static class CareGuideJson
{
    private static readonly JsonSerializerOptions Options = new(JsonHelper.DefaultOptions)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static bool LooksLikeJson(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.TrimStart().StartsWith('{');

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static SpeciesCareGuideDto? TryParseSpeciesGuide(string? raw)
    {
        if (!LooksLikeJson(raw))
        {
            return null;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<SpeciesCareGuideDto>(raw!, Options);
            if (dto == null)
            {
                return null;
            }

            NormalizeToV3(dto);

            if (!dto.HasArticleContent() && string.IsNullOrWhiteSpace(dto.ChineseName) && dto.QuickFacts?.HasAny() != true)
            {
                return null;
            }

            dto.Schema = SpeciesCareGuideDto.SchemaVersion;
            return dto;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>把 v2 扁平段落升成 modules；清掉廢止的 carePriority。</summary>
    public static void NormalizeToV3(SpeciesCareGuideDto dto)
    {
        dto.FertilizerRecipe ??= dto.Fertilizer;

        if (dto.Modules.Count == 0)
        {
            AddModule(dto, CareGuideModuleIds.BasicInfo, null, dto.Basics);
            AddModule(dto, CareGuideModuleIds.Identification,
                FirstNonEmpty(dto.Identification, dto.Summary));
            // carePriority 刻意不升成模組
            AddModule(dto, CareGuideModuleIds.Light, dto.Light);
            AddModule(dto, CareGuideModuleIds.Watering, dto.Watering);
            AddModule(dto, CareGuideModuleIds.Temperature, dto.Temperature);
            AddModule(dto, CareGuideModuleIds.Soil, dto.Substrate);
            AddModule(dto, CareGuideModuleIds.Propagation, dto.Propagation);
            AddModule(dto, CareGuideModuleIds.Flowering, dto.Flowering);
            AddModule(dto, CareGuideModuleIds.Lookalikes, dto.Lookalikes);
            if (dto.FertilizerRecipe != null || dto.Fertilizer != null)
            {
                // 施肥原則若無文，至少留空模組標記有配方——詳情用配方卡
                if (dto.FindModule(CareGuideModuleIds.Fertilizer) == null)
                {
                    dto.Modules.Add(new CareKnowledgeModuleDto { Id = CareGuideModuleIds.Fertilizer });
                }
            }
        }
        else
        {
            foreach (var mod in dto.Modules)
            {
                var mapped = CareGuideSectionOrderDto.MapLegacySectionKey(mod.Id);
                if (!string.IsNullOrWhiteSpace(mapped))
                {
                    mod.Id = mapped;
                }
            }

            // 確保 basic-info 可帶 basics
            var basic = dto.FindModule(CareGuideModuleIds.BasicInfo);
            if (basic != null && basic.Basics == null && dto.Basics != null)
            {
                basic.Basics = dto.Basics;
            }

            // 舊扁平 substrate 尚未升模組時補上
            if (dto.FindModule(CareGuideModuleIds.Soil) == null)
            {
                AddModule(dto, CareGuideModuleIds.Soil, dto.Substrate);
            }
        }

        // 精簡序列化用：清掉會重複的 v2 欄（寫回時可不帶）
        dto.CarePriority = null;
    }

    /// <summary>寫入 DB 前：只保留 v3 欄位，避免雙寫。</summary>
    public static SpeciesCareGuideDto ForStorage(SpeciesCareGuideDto source)
    {
        NormalizeToV3(source);
        return new SpeciesCareGuideDto
        {
            Schema = SpeciesCareGuideDto.SchemaVersion,
            ChineseName = source.ChineseName,
            ScientificName = source.ScientificName,
            QuickFacts = source.QuickFacts,
            Modules = source.Modules
                .Where(m => m.HasContent() || string.Equals(m.Id, CareGuideModuleIds.Fertilizer, StringComparison.OrdinalIgnoreCase))
                .Select(m => new CareKnowledgeModuleDto
                {
                    Id = m.Id,
                    Content = NullIfEmpty(m.Content),
                    Basics = m.Basics,
                    Warnings = m.Warnings?.Where(w => !string.IsNullOrWhiteSpace(w)).Select(w => w.Trim()).ToList() ?? [],
                    Tips = m.Tips?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList() ?? []
                })
                .Where(m => m.HasContent() || string.Equals(m.Id, CareGuideModuleIds.Fertilizer, StringComparison.OrdinalIgnoreCase))
                .ToList(),
            FertilizerRecipe = source.FertilizerRecipe ?? source.Fertilizer
        };
    }

    public static void ProjectQuickFactsToKnowledge(
        CareQuickFactsDto? facts,
        Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel knowledge)
    {
        if (facts == null || !facts.HasAny()) return;

        if (facts.Light.HasValue)
        {
            knowledge.SuggestedLight = facts.Light;
            knowledge.LightRequirement = LightLevelDisplay.ToLabel(facts.Light);
        }

        if (!string.IsNullOrWhiteSpace(facts.WateringStrategy) || !string.IsNullOrWhiteSpace(facts.WateringIntervalHint))
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(facts.WateringStrategy))
            {
                parts.Add(HumanizeWateringStrategy(facts.WateringStrategy));
            }

            if (!string.IsNullOrWhiteSpace(facts.WateringIntervalHint))
            {
                parts.Add($"參考間隔 {facts.WateringIntervalHint.Trim()}");
            }

            knowledge.WaterRequirement = string.Join("；", parts);
            knowledge.SuggestedWateringIntervalDays = ParseIntervalDays(facts.WateringIntervalHint);
        }

        if (!string.IsNullOrWhiteSpace(facts.Humidity))
        {
            knowledge.HumidityRequirement = HumanizeHumidity(facts.Humidity);
        }

        if (facts.TemperatureMin.HasValue) knowledge.TemperatureMin = facts.TemperatureMin;
        if (facts.TemperatureMax.HasValue) knowledge.TemperatureMax = facts.TemperatureMax;

        if (facts.GrowingSeason.Count > 0)
        {
            knowledge.GrowthSeason = string.Join("、", facts.GrowingSeason.Select(HumanizeSeason));
        }
    }

    /// <summary>有 soil／substrate 模組內容但短欄空白時，投影到 SoilRequirement。</summary>
    public static void ProjectSoilModuleToKnowledge(
        SpeciesCareGuideDto? guide,
        Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel knowledge)
    {
        if (knowledge == null || !string.IsNullOrWhiteSpace(knowledge.SoilRequirement) || guide == null)
        {
            return;
        }

        var soil = guide.FindModule(CareGuideModuleIds.Soil);
        var text = FirstNonEmpty(soil?.Content, guide.Substrate);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var trimmed = text.Trim();
        knowledge.SoilRequirement = trimmed.Length > 400 ? trimmed[..400] + "…" : trimmed;
    }

    private static void AddModule(SpeciesCareGuideDto dto, string id, string? content, CareBasicsDto? basics = null)
    {
        if (string.IsNullOrWhiteSpace(content) && !SpeciesCareGuideDto.BasicsHasContent(basics))
        {
            return;
        }

        if (dto.FindModule(id) != null) return;
        dto.Modules.Add(new CareKnowledgeModuleDto
        {
            Id = id,
            Content = NullIfEmpty(content),
            Basics = basics
        });
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string HumanizeWateringStrategy(string raw)
    {
        var t = raw.Trim().ToLowerInvariant();
        return t switch
        {
            "dry_then_soak" or "dry-then-soak" or "dry_then_water" => "介質乾透後再澆透",
            "keep_moist" => "保持微濕、勿積水",
            "sparse" => "偏乾養、少量給水",
            _ => raw.Trim()
        };
    }

    private static string HumanizeHumidity(string raw)
    {
        var t = raw.Trim().ToLowerInvariant();
        return t switch
        {
            "low" => "低濕",
            "medium" or "moderate" => "中濕",
            "high" => "高濕",
            _ => raw.Trim()
        };
    }

    private static string HumanizeSeason(string raw)
    {
        var t = raw.Trim().ToLowerInvariant();
        return t switch
        {
            "spring" => "春",
            "summer" => "夏",
            "autumn" or "fall" => "秋",
            "winter" => "冬",
            _ => raw.Trim()
        };
    }

    private static int? ParseIntervalDays(string? hint)
    {
        if (string.IsNullOrWhiteSpace(hint)) return null;
        var nums = System.Text.RegularExpressions.Regex.Matches(hint, @"\d{1,3}")
            .Select(m => int.Parse(m.Value))
            .Where(n => n is >= 1 and <= 90)
            .ToList();
        if (nums.Count == 0) return null;
        if (nums.Count == 1) return nums[0];
        return (int)Math.Round((nums.Min() + nums.Max()) / 2.0);
    }

    public static EnvironmentAdviceDto? TryParseEnvironmentAdvice(string? raw)
    {
        if (!LooksLikeJson(raw))
        {
            return null;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<EnvironmentAdviceDto>(raw!, Options);
            if (dto == null || (string.IsNullOrWhiteSpace(dto.FitSummary)
                && dto.Problems.Count == 0 && dto.Adjustments.Count == 0
                && string.IsNullOrWhiteSpace(dto.FertilizerAdjustment)))
            {
                return null;
            }

            return dto;
        }
        catch
        {
            return null;
        }
    }

    public static string FormatFertilizerRequirement(FertilizerRecipeDto? f)
    {
        if (f == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(f.Type)) parts.Add(f.Type.Trim());
        if (!string.IsNullOrWhiteSpace(f.NpkHint)) parts.Add($"NPK {f.NpkHint.Trim()}");
        if (!string.IsNullOrWhiteSpace(f.Dilution)) parts.Add($"稀釋 {f.Dilution.Trim()}");
        if (!string.IsNullOrWhiteSpace(f.Frequency)) parts.Add(f.Frequency.Trim());
        if (!string.IsNullOrWhiteSpace(f.Notes)) parts.Add(f.Notes.Trim());
        return string.Join("；", parts);
    }

    public static (int Strong, int Mild) ResolveDilutionRange(FertilizerRecipeDto? f)
    {
        if (f?.DilutionStrong is > 0 && f.DilutionMild is > 0)
        {
            var a = Math.Min(f.DilutionStrong.Value, f.DilutionMild.Value);
            var b = Math.Max(f.DilutionStrong.Value, f.DilutionMild.Value);
            return (a, b);
        }

        var numbers = System.Text.RegularExpressions.Regex.Matches(f?.Dilution ?? string.Empty, @"\d{3,5}")
            .Select(m => int.Parse(m.Value))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        if (numbers.Count >= 2)
        {
            return (numbers[0], numbers[^1]);
        }

        if (numbers.Count == 1)
        {
            var n = numbers[0];
            return (n, Math.Max(n * 2, n + 500));
        }

        return (1000, 2000);
    }

    public static bool HasCjk(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.Any(c => c >= 0x4E00 && c <= 0x9FFF);

    public static string? TrustedChineseName(string? userOrKeyword, string? databaseChinese)
    {
        if (HasCjk(userOrKeyword))
        {
            return userOrKeyword!.Trim();
        }

        if (HasCjk(databaseChinese))
        {
            return databaseChinese!.Trim();
        }

        return null;
    }
}

namespace Midnight.EC.Plant.WEB.Models.External;

/// <summary>照護知識模組穩定 id（模組登錄）。</summary>
public static class CareGuideModuleIds
{
    public const string BasicInfo = "basic-info";
    public const string Identification = "identification";
    public const string Light = "light";
    public const string Watering = "watering";
    public const string Humidity = "humidity";
    public const string Temperature = "temperature";
    public const string Soil = "soil";
    public const string Fertilizer = "fertilizer";
    public const string GrowingSeason = "growing-season";
    public const string Flowering = "flowering";
    public const string Propagation = "propagation";
    public const string Lookalikes = "lookalikes";
    public const string EnvironmentAnalysis = "environment-analysis";

    /// <summary>會出現在物種 JSON 的模組（不含個人／通用）。</summary>
    public static readonly string[] SpeciesContentIds =
    [
        BasicInfo,
        Identification,
        Light,
        Watering,
        Humidity,
        Temperature,
        Soil,
        Fertilizer,
        GrowingSeason,
        Flowering,
        Propagation,
        Lookalikes
    ];
}

public enum CareModuleCategory
{
    Species,
    Care,
    Growth,
    Management,
    Health,
    Personal,
    Shared
}

public sealed class CareModuleDefinition
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required CareModuleCategory Category { get; init; }
    public required int DefaultOrder { get; init; }
    public bool IsPersonal { get; init; }
    public bool IsShared { get; init; }
}

/// <summary>系統模組登錄：可有哪些模組、預設順序與標題。</summary>
public static class CareGuideModuleRegistry
{
    public static readonly CareModuleDefinition[] All =
    [
        new() { Id = CareGuideModuleIds.BasicInfo, Title = "基本資料", Category = CareModuleCategory.Species, DefaultOrder = 10 },
        new() { Id = CareGuideModuleIds.Identification, Title = "辨識與特色", Category = CareModuleCategory.Species, DefaultOrder = 20 },
        new() { Id = CareGuideModuleIds.Light, Title = "光照", Category = CareModuleCategory.Care, DefaultOrder = 30 },
        new() { Id = CareGuideModuleIds.Watering, Title = "澆水", Category = CareModuleCategory.Care, DefaultOrder = 40 },
        new() { Id = CareGuideModuleIds.Humidity, Title = "濕度", Category = CareModuleCategory.Care, DefaultOrder = 50 },
        new() { Id = CareGuideModuleIds.Temperature, Title = "溫度", Category = CareModuleCategory.Care, DefaultOrder = 60 },
        new() { Id = CareGuideModuleIds.Soil, Title = "介質", Category = CareModuleCategory.Care, DefaultOrder = 70 },
        new() { Id = CareGuideModuleIds.Fertilizer, Title = "施肥", Category = CareModuleCategory.Care, DefaultOrder = 80 },
        new() { Id = CareGuideModuleIds.GrowingSeason, Title = "生長季", Category = CareModuleCategory.Growth, DefaultOrder = 90 },
        new() { Id = CareGuideModuleIds.Flowering, Title = "開花／催花", Category = CareModuleCategory.Growth, DefaultOrder = 100 },
        new() { Id = CareGuideModuleIds.Propagation, Title = "繁殖", Category = CareModuleCategory.Management, DefaultOrder = 110 },
        new() { Id = CareGuideModuleIds.Lookalikes, Title = "易混種", Category = CareModuleCategory.Species, DefaultOrder = 170 },
        new() { Id = CareGuideModuleIds.EnvironmentAnalysis, Title = "我的環境", Category = CareModuleCategory.Personal, DefaultOrder = 200, IsPersonal = true }
    ];

    public static CareModuleDefinition? Find(string? id) =>
        string.IsNullOrWhiteSpace(id)
            ? null
            : All.FirstOrDefault(m => string.Equals(m.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string TitleOf(string id) => Find(id)?.Title ?? id;

    public static bool IsKnown(string id) => Find(id) != null;
}

public class CareGuideLayoutEntryDto
{
    public string ModuleId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
}

/// <summary>全站模組版面（順序＋開關）；與物種內容分離。</summary>
public class CareGuideSectionOrderDto
{
    public const int CurrentVersion = 2;
    public const string LayoutKey = "care-guide-sections";

    public int Version { get; set; } = CurrentVersion;

    /// <summary>舊版僅字串陣列；讀取時會升成 Entries。</summary>
    public List<string>? Sections { get; set; }

    public List<CareGuideLayoutEntryDto> Entries { get; set; } = [];

    public static CareGuideSectionOrderDto Normalize(CareGuideSectionOrderDto? raw)
    {
        var result = new CareGuideSectionOrderDto { Version = CurrentVersion };
        var byId = new Dictionary<string, CareGuideLayoutEntryDto>(StringComparer.OrdinalIgnoreCase);

        void Upsert(string id, bool enabled, int order)
        {
            if (!CareGuideModuleRegistry.IsKnown(id)) return;
            var canon = CareGuideModuleRegistry.Find(id)!.Id;
            if (byId.ContainsKey(canon)) return;
            byId[canon] = new CareGuideLayoutEntryDto
            {
                ModuleId = canon,
                Enabled = enabled,
                Order = order
            };
        }

        // 新格式
        if (raw?.Entries is { Count: > 0 })
        {
            var ordered = raw.Entries
                .Where(e => e != null && !string.IsNullOrWhiteSpace(e.ModuleId))
                .OrderBy(e => e.Order)
                .ThenBy(e => e.ModuleId, StringComparer.OrdinalIgnoreCase);
            var i = 0;
            foreach (var e in ordered)
            {
                Upsert(e.ModuleId, e.Enabled, (i + 1) * 10);
                i++;
            }
        }
        // 舊 Sections 字串
        else if (raw?.Sections is { Count: > 0 })
        {
            var i = 0;
            foreach (var key in raw.Sections)
            {
                var mapped = MapLegacyKey(key);
                if (mapped == null) continue;
                Upsert(mapped, true, (i + 1) * 10);
                i++;
            }
        }

        // 補齊登錄內缺漏（預設順序、預設啟用）
        foreach (var def in CareGuideModuleRegistry.All.OrderBy(d => d.DefaultOrder))
        {
            if (byId.ContainsKey(def.Id)) continue;
            byId[def.Id] = new CareGuideLayoutEntryDto
            {
                ModuleId = def.Id,
                Enabled = true,
                Order = def.DefaultOrder
            };
        }

        result.Entries = byId.Values
            .OrderBy(e => e.Order)
            .ThenBy(e => e.ModuleId, StringComparer.OrdinalIgnoreCase)
            .Select((e, idx) =>
            {
                e.Order = (idx + 1) * 10;
                return e;
            })
            .ToList();

        result.Sections = null;
        return result;
    }

    public static string? MapLegacySectionKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var k = key.Trim();
        return k.ToLowerInvariant() switch
        {
            "basics" or "basic-info" => CareGuideModuleIds.BasicInfo,
            "identification" => CareGuideModuleIds.Identification,
            "carepriority" or "care-summary" or "care-priority" => null,
            "light" => CareGuideModuleIds.Light,
            "watering" => CareGuideModuleIds.Watering,
            "humidity" => CareGuideModuleIds.Humidity,
            "temperature" => CareGuideModuleIds.Temperature,
            "substrate" or "soil" => CareGuideModuleIds.Soil,
            "fertilizer" => CareGuideModuleIds.Fertilizer,
            "growing-season" or "growthseason" => CareGuideModuleIds.GrowingSeason,
            "flowering" => CareGuideModuleIds.Flowering,
            "propagation" => CareGuideModuleIds.Propagation,
            "lookalikes" => CareGuideModuleIds.Lookalikes,
            "environment-analysis" => CareGuideModuleIds.EnvironmentAnalysis,
            _ => CareGuideModuleRegistry.IsKnown(k) ? CareGuideModuleRegistry.Find(k)!.Id : null
        };
    }

    private static string? MapLegacyKey(string? key) => MapLegacySectionKey(key);
}

/// <summary>舊 API 相容別名。</summary>
[Obsolete("Use CareGuideModuleIds / CareGuideModuleRegistry")]
public static class CareGuideSectionKeys
{
    public const string Basics = CareGuideModuleIds.BasicInfo;
    public const string Identification = CareGuideModuleIds.Identification;
    public const string CarePriority = "carePriority";
    public const string Light = CareGuideModuleIds.Light;
    public const string Watering = CareGuideModuleIds.Watering;
    public const string Substrate = CareGuideModuleIds.Soil;
    public const string Temperature = CareGuideModuleIds.Temperature;
    public const string Propagation = CareGuideModuleIds.Propagation;
    public const string Flowering = CareGuideModuleIds.Flowering;
    public const string Lookalikes = CareGuideModuleIds.Lookalikes;
    public const string Fertilizer = CareGuideModuleIds.Fertilizer;

    public static readonly string[] DefaultOrder = CareGuideModuleIds.SpeciesContentIds;

    public static string DisplayName(string key)
    {
        var mapped = CareGuideSectionOrderDto.MapLegacySectionKey(key);
        return mapped == null ? key : CareGuideModuleRegistry.TitleOf(mapped);
    }
}

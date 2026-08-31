using Midnight.EC.Plant.WEB.Models.External;

namespace Midnight.EC.Plant.WEB.Services.External;

public static class GenusCareTemplateProvider
{
    public static ExternalKnowledgePartial? TryGetFromKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return null;
        }

        var latin = ChineseKeywordExpander.TryResolvePrimaryLatinName(keyword);
        if (latin != null)
        {
            var genus = latin.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return TryGet(genus, null);
        }

        return null;
    }

    public static ExternalKnowledgePartial? TryGet(string? genus, string? family)
    {
        if (string.IsNullOrWhiteSpace(genus))
        {
            return null;
        }

        return genus.Trim().ToLowerInvariant() switch
        {
            "haworthia" or "haworthiopsis" => SucculentTemplate(
                "Haworthia 玉露/十二卷類",
                "明亮散射光，可短時間柔和直射；錦斑品種避免強烈午後直射。",
                "土表乾燥後再澆透，冬季明顯減水；忌葉心長期積水。",
                "40–60%，通風良好即可。",
                10m, 28m,
                "顆粒土為主、排水佳；可混少量泥炭。",
                "生長季薄肥，1–2 個月一次；冬季停肥。"),

            "echeveria" => SucculentTemplate(
                "Echeveria 石蓮類",
                "全日照至半日照；室內需足夠亮處。",
                "乾透再澆；夏季高溫需控水防黑腐。",
                "40–50%。",
                5m, 30m,
                "疏水顆粒土。",
                "春秋季薄肥。"),

            "crassula" or "sedum" => SucculentTemplate(
                "Crassula / Sedum 景天類",
                "半日照至全日照。",
                "土乾再澆；耐旱。",
                "40–60%。",
                5m, 32m,
                "排水良好介質。",
                "生長季低頻施肥。"),

            _ when string.Equals(family, "Cactaceae", StringComparison.OrdinalIgnoreCase) => SucculentTemplate(
                "仙人掌科",
                "充足光照。",
                "乾透再澆；冬季休眠少水。",
                "30–50%。",
                5m, 35m,
                "仙人掌專用土。",
                "生長季薄肥。"),

            _ => null
        };
    }

    private static ExternalKnowledgePartial SucculentTemplate(
        string label,
        string light,
        string water,
        string humidity,
        decimal tempMin,
        decimal tempMax,
        string soil,
        string fertilizer) => new()
    {
        LightRequirement = light,
        WaterRequirement = water,
        HumidityRequirement = humidity,
        TemperatureMin = tempMin,
        TemperatureMax = tempMax,
        SoilRequirement = soil,
        FertilizerRequirement = fertilizer,
        CareSummary = $"依 {label} 一般栽培慣例提供的參考建議（非該個體保證值，錦斑/個體差異請再觀察調整）。",
        Provider = "GenusTemplate"
    };
}

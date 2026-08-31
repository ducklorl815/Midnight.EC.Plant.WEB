using Midnight.EC.Plant.WEB.Models.External;

namespace Midnight.EC.Plant.WEB.Services.External;

public static class ExternalKnowledgeMerger
{
    public static ExternalKnowledgeResult Merge(params ExternalKnowledgePartial?[] sources)
    {
        var result = new ExternalKnowledgeResult();
        var providers = new List<string>();

        foreach (var source in sources.Where(s => s != null))
        {
            if (string.IsNullOrWhiteSpace(result.LightRequirement) && !string.IsNullOrWhiteSpace(source!.LightRequirement))
                result.LightRequirement = source.LightRequirement;
            if (string.IsNullOrWhiteSpace(result.WaterRequirement) && !string.IsNullOrWhiteSpace(source.WaterRequirement))
                result.WaterRequirement = source.WaterRequirement;
            if (string.IsNullOrWhiteSpace(result.HumidityRequirement) && !string.IsNullOrWhiteSpace(source.HumidityRequirement))
                result.HumidityRequirement = source.HumidityRequirement;
            if (string.IsNullOrWhiteSpace(result.SoilRequirement) && !string.IsNullOrWhiteSpace(source.SoilRequirement))
                result.SoilRequirement = source.SoilRequirement;
            if (string.IsNullOrWhiteSpace(result.FertilizerRequirement) && !string.IsNullOrWhiteSpace(source.FertilizerRequirement))
                result.FertilizerRequirement = source.FertilizerRequirement;
            if (string.IsNullOrWhiteSpace(result.GrowthSeason) && !string.IsNullOrWhiteSpace(source.GrowthSeason))
                result.GrowthSeason = source.GrowthSeason;

            if (!result.TemperatureMin.HasValue && source.TemperatureMin.HasValue)
            {
                result.TemperatureMin = source.TemperatureMin;
            }

            if (!result.TemperatureMax.HasValue && source.TemperatureMax.HasValue)
            {
                result.TemperatureMax = source.TemperatureMax;
            }

            if (!string.IsNullOrWhiteSpace(source.CareSummary))
            {
                result.CareSummary = string.IsNullOrWhiteSpace(result.CareSummary)
                    ? source.CareSummary
                    : $"{result.CareSummary} {source.CareSummary}";
            }

            if (!string.IsNullOrWhiteSpace(source.Provider) && !providers.Contains(source.Provider))
            {
                providers.Add(source.Provider);
            }
        }

        if (providers.Count > 0)
        {
            var prefix = $"資料來源：{string.Join("、", providers)}。";
            result.CareSummary = string.IsNullOrWhiteSpace(result.CareSummary)
                ? prefix
                : result.CareSummary.Contains(prefix, StringComparison.Ordinal) ? result.CareSummary : $"{prefix} {result.CareSummary}";
            result.Provider = string.Join("+", providers);
        }

        return result;
    }

    public static ExternalKnowledgePartial ToPartial(ExternalKnowledgeResult result) => new()
    {
        LightRequirement = result.LightRequirement,
        WaterRequirement = result.WaterRequirement,
        HumidityRequirement = result.HumidityRequirement,
        TemperatureMin = result.TemperatureMin,
        TemperatureMax = result.TemperatureMax,
        SoilRequirement = result.SoilRequirement,
        FertilizerRequirement = result.FertilizerRequirement,
        GrowthSeason = result.GrowthSeason,
        CareSummary = result.CareSummary,
        Provider = result.Provider
    };
}

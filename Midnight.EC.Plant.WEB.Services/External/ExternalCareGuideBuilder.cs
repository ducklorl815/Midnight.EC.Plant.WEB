using System.Text;
using Midnight.EC.Plant.WEB.Models.External;

namespace Midnight.EC.Plant.WEB.Services.External;

public static class ExternalCareGuideBuilder
{
    public static string? Build(string plantDisplayName, ExternalKnowledgeResult knowledge)
    {
        if (string.IsNullOrWhiteSpace(plantDisplayName))
        {
            plantDisplayName = "此植栽";
        }

        var sections = new List<string>();
        AppendSection(sections, "光照", knowledge.LightRequirement);
        AppendSection(sections, "澆水", knowledge.WaterRequirement);
        AppendSection(sections, "土壤", knowledge.SoilRequirement);
        AppendSection(sections, "施肥", knowledge.FertilizerRequirement);

        var climate = BuildClimateSection(knowledge);
        if (!string.IsNullOrWhiteSpace(climate))
        {
            sections.Add(climate);
        }

        if (sections.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{plantDisplayName.Trim()}照護要點");
        sb.Append(string.Join('\n', sections));

        return sb.ToString().Trim();
    }

    /// <summary>移除資料來源等後設資訊，僅保留可讀照護說明。</summary>
    public static string? SanitizeForDisplay(string? guide)
    {
        if (string.IsNullOrWhiteSpace(guide))
        {
            return guide;
        }

        var idx = guide.IndexOf("資料來源", StringComparison.Ordinal);
        if (idx >= 0)
        {
            guide = guide[..idx];
        }

        var lines = guide
            .Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Where(l => !l.StartsWith("資料來源", StringComparison.Ordinal))
            .Where(l => !(l.StartsWith("依 ", StringComparison.Ordinal) && l.Contains("一般栽培慣例", StringComparison.Ordinal)))
            .ToList();

        var result = string.Join('\n', lines).Trim().TrimEnd('。', '.', ' ', '　');
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    public static string? BuildFromPartial(string plantDisplayName, ExternalKnowledgePartial partial) =>
        Build(plantDisplayName, new ExternalKnowledgeResult
        {
            LightRequirement = partial.LightRequirement,
            WaterRequirement = partial.WaterRequirement,
            HumidityRequirement = partial.HumidityRequirement,
            TemperatureMin = partial.TemperatureMin,
            TemperatureMax = partial.TemperatureMax,
            SoilRequirement = partial.SoilRequirement,
            FertilizerRequirement = partial.FertilizerRequirement,
            CareSummary = partial.CareSummary,
            Provider = partial.Provider
        });

    private static void AppendSection(List<string> sections, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sections.Add($"{label}：{value.Trim()}");
        }
    }

    private static string? BuildClimateSection(ExternalKnowledgeResult knowledge)
    {
        var parts = new List<string>();

        if (knowledge.TemperatureMin.HasValue || knowledge.TemperatureMax.HasValue)
        {
            var min = knowledge.TemperatureMin?.ToString("0.#") ?? "?";
            var max = knowledge.TemperatureMax?.ToString("0.#") ?? "?";
            parts.Add($"適合生長溫度為 {min}~{max}℃");
        }

        if (!string.IsNullOrWhiteSpace(knowledge.HumidityRequirement))
        {
            parts.Add(knowledge.HumidityRequirement.Trim());
        }

        if (parts.Count == 0)
        {
            return null;
        }

        return $"溫度與濕度：{string.Join("；", parts)}。";
    }
}

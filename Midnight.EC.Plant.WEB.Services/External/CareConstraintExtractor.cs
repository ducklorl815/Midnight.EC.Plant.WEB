using System.Text.Json;
using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Services.External;

public static class CareConstraintExtractor
{
    private static readonly (string Needle, string Label)[] TabooRules =
    [
        ("忌烈日", "忌烈日"),
        ("怕烈日", "忌烈日"),
        ("忌強光", "忌烈日"),
        ("遮雨", "需遮雨"),
        ("避雨", "需遮雨"),
        ("怕雨", "需遮雨"),
        ("忌積水", "忌積水"),
        ("怕積水", "忌積水"),
        ("忌涝", "忌積水"),
        ("排水", "需排水良好"),
        ("忌直晒", "忌烈日"),
        ("不可直晒", "忌烈日"),
        ("喜陰", "忌烈日"),
        ("不耐寒", "不耐寒"),
        ("忌寒", "不耐寒")
    ];

    public static List<string> ExtractTaboos(params string?[] texts)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (var text in texts)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            foreach (var (needle, label) in TabooRules)
            {
                if (text.Contains(needle, StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(label);
                }
            }
        }

        return found.ToList();
    }

    public static string? ToJson(IEnumerable<string> taboos)
    {
        var list = taboos.Distinct().ToList();
        return list.Count == 0 ? null : JsonSerializer.Serialize(list);
    }

    public static List<string> FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static int? InferWateringIntervalDays(string? waterRequirement)
    {
        if (string.IsNullOrWhiteSpace(waterRequirement))
        {
            return null;
        }

        var m = System.Text.RegularExpressions.Regex.Match(waterRequirement, @"(\d+)\s*天");
        if (m.Success && int.TryParse(m.Groups[1].Value, out var days) && days is > 0 and < 365)
        {
            return days;
        }

        if (waterRequirement.Contains("每週", StringComparison.Ordinal) ||
            waterRequirement.Contains("一周", StringComparison.Ordinal) ||
            waterRequirement.Contains("一週", StringComparison.Ordinal))
        {
            return 7;
        }

        return null;
    }

    public static List<string> BuildMismatchWarnings(
        LightLevel? suggestedLight,
        LightLevel? actualLight,
        IReadOnlyList<string> taboos,
        bool? hasRainCover,
        PlacementType? placement,
        string? substrateType)
    {
        var warnings = new List<string>();

        if (suggestedLight.HasValue && actualLight.HasValue && suggestedLight != actualLight)
        {
            warnings.Add($"建議日照「{LightLevelDisplay.ToLabel(suggestedLight)}」，實際為「{LightLevelDisplay.ToLabel(actualLight)}」。");
        }

        foreach (var taboo in taboos)
        {
            if (taboo == "忌烈日" && actualLight == LightLevel.FullSun)
            {
                warnings.Add("知識標示忌烈日，但實際日照為烈日。");
            }

            if (taboo == "需遮雨" && hasRainCover == false)
            {
                warnings.Add("知識標示需遮雨，但實際設定為無遮雨。");
            }

            if (taboo == "忌積水" &&
                !string.IsNullOrWhiteSpace(substrateType) &&
                (substrateType.Contains("保水", StringComparison.Ordinal) ||
                 substrateType.Contains("黏土", StringComparison.Ordinal)))
            {
                warnings.Add($"知識標示忌積水，但介質為「{substrateType}」。");
            }

            if (taboo == "忌烈日" && placement == PlacementType.Outdoor && actualLight == LightLevel.FullSun)
            {
                warnings.Add("室外＋烈日，與忌烈日建議衝突。");
            }
        }

        return warnings.Distinct().ToList();
    }
}

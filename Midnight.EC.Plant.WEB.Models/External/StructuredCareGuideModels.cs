using System.Text.Json;
using System.Text.Json.Serialization;
using Midnight.EC.Plant.WEB.Utility.Json;

namespace Midnight.EC.Plant.WEB.Models.External;

/// <summary>物種照護說明（結構化，存於 PlantKnowledge.ExternalCareGuide）。</summary>
public class SpeciesCareGuideDto
{
    public const string SchemaVersion = "species-guide-v1";

    public string Schema { get; set; } = SchemaVersion;
    public string? ChineseName { get; set; }
    public string? ScientificName { get; set; }
    public string? Summary { get; set; }
    public FertilizerRecipeDto? Fertilizer { get; set; }
}

public class FertilizerRecipeDto
{
    /// <summary>肥種，例如平衡液肥、觀葉肥</summary>
    public string? Type { get; set; }
    /// <summary>NPK 參考，例如 20-20-20</summary>
    public string? NpkHint { get; set; }
    /// <summary>稀釋比例，例如 1000–2000 倍</summary>
    public string? Dilution { get; set; }
    /// <summary>較濃端倍數（可選，供表格）</summary>
    public int? DilutionStrong { get; set; }
    /// <summary>較淡端倍數（可選，供表格）</summary>
    public int? DilutionMild { get; set; }
    /// <summary>施肥頻率</summary>
    public string? Frequency { get; set; }
    public string? Notes { get; set; }
}

/// <summary>針對實際環境的適配建議（結構化，存於 PlantProfile.AiEnvironmentAdvice）。</summary>
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
            if (dto == null || (string.IsNullOrWhiteSpace(dto.Summary) && dto.Fertilizer == null
                && string.IsNullOrWhiteSpace(dto.ChineseName)))
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

    /// <summary>解析稀釋倍數區間；缺省時給新手友善的 1000–2000。</summary>
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

    /// <summary>中文名只信任使用者輸入或資料庫，絕不採用 AI 自造俗名。</summary>
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

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.External;

namespace Midnight.EC.Plant.WEB.Services.AI;

public sealed class EnvironmentFitService
{
    private readonly IChatCompletionEnvelope _chat;
    private readonly ILogger<EnvironmentFitService> _logger;

    public EnvironmentFitService(
        IChatCompletionEnvelope chat,
        ILogger<EnvironmentFitService> logger)
    {
        _chat = chat;
        _logger = logger;
    }

    public async Task<EnvironmentFitResult> RefreshAsync(
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
            return EnvironmentFitResult.Skipped();

        try
        {
            var userPrompt = BuildPrompt(plantDisplayName, scientificName, knowledge, environment);
            var content = await _chat.CompleteAsync(
                [
                    new ChatCompletionMessage
                    {
                        Role = "system",
                        Text = """
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
                    new ChatCompletionMessage { Role = "user", Text = userPrompt }
                ],
                cancellationToken: cancellationToken);

            using var adviceDoc = JsonDocument.Parse(content);
            var root = adviceDoc.RootElement;

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

            var advice = GetString(root, "advice");
            if (string.IsNullOrWhiteSpace(advice))
                advice = content.Trim();

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

    private static string BuildPrompt(
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
                sb.AppendLine($"- {w}");
        }

        sb.AppendLine("【物種需求摘要】只作對照，不要重抄百科");
        sb.AppendLine($"- 光照：{knowledge.LightRequirement}");
        sb.AppendLine($"- 澆水：{knowledge.WaterRequirement}");
        sb.AppendLine($"- 濕度：{knowledge.HumidityRequirement}");
        sb.AppendLine($"- 溫度：{knowledge.TemperatureMin}~{knowledge.TemperatureMax}°C");
        sb.AppendLine($"- 介質：{knowledge.SoilRequirement}");
        sb.AppendLine($"- 施肥：{knowledge.FertilizerRequirement}");
        if (!string.IsNullOrWhiteSpace(knowledge.ExternalCareGuide))
        {
            var guide = knowledge.ExternalCareGuide.Length > 600
                ? knowledge.ExternalCareGuide[..600] + "…"
                : knowledge.ExternalCareGuide;
            sb.AppendLine($"- 物種說明摘要：{guide}");
        }

        sb.AppendLine("請產出結構化適配分析（fitSummary／problems／adjustments／fertilizerAdjustment），繁體中文。");
        return sb.ToString();
    }

    private static List<EnvironmentProblemDto> ParseProblems(JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "problems", out var el) || el.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<EnvironmentProblemDto>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var text = item.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    list.Add(new EnvironmentProblemDto { Text = text.Trim() });
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var textVal = NullIfEmpty(GetString(item, "text")) ?? NullIfEmpty(GetString(item, "problem"));
            if (string.IsNullOrWhiteSpace(textVal))
                continue;

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
            return [];

        return el.EnumerateArray()
            .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToList();
    }

    private static string? GetString(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el))
            return null;

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

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string name, out JsonElement value)
    {
        if (root.TryGetProperty(name, out value))
            return true;

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

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

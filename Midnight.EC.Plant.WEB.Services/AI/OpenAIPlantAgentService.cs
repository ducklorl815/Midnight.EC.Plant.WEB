using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.AI;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Utility.Json;

namespace Midnight.EC.Plant.WEB.Services.AI;

public class OpenAIPlantAgentService
{
    public const string PromptVersion = "plant-analysis-v9";

    private readonly IChatCompletionEnvelope _chat;
    private readonly ILogger<OpenAIPlantAgentService> _logger;

    public OpenAIPlantAgentService(
        IChatCompletionEnvelope chat,
        ILogger<OpenAIPlantAgentService> logger)
    {
        _chat = chat;
        _logger = logger;
    }

    public async Task<PlantAnalysisResultDto> AnalyzePlantAsync(PlantAnalysisContext context, CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(context);
        ChatCompletionMessage userMessage;
        if (!string.IsNullOrWhiteSpace(context.FocusImageAbsolutePath) && File.Exists(context.FocusImageAbsolutePath))
        {
            var bytes = await File.ReadAllBytesAsync(context.FocusImageAbsolutePath, cancellationToken);
            var base64 = Convert.ToBase64String(bytes);
            var mime = context.FocusImageContentType ?? "image/jpeg";
            userMessage = new ChatCompletionMessage
            {
                Role = "user",
                Parts =
                [
                    ChatContentPart.FromText(prompt),
                    ChatContentPart.FromImageDataUrl($"data:{mime};base64,{base64}")
                ]
            };
        }
        else
        {
            userMessage = new ChatCompletionMessage { Role = "user", Text = prompt };
        }

        try
        {
            var content = await _chat.CompleteAsync(
                [
                    new ChatCompletionMessage
                    {
                        Role = "system",
                        Text = """
                            你是植栽照護助理，服務對象為台灣／繁體中文使用者。
                            【語言強制】所有給使用者閱讀的文字必須使用繁體中文（台灣用字），禁止簡體中文；學名、URL、數字可維持原文。
                            JSON 欄位名稱維持英文字鍵；欄位值用繁體中文。
                            命名：以學名為準；若有使用者中文名可用，勿自行發明其他中文俗名。
                            【診斷優先】這是針對該盆／該張照片的診斷，不是百科罐頭文。summary 只需一句導語；細節放在陣列欄位。
                            必須區分：observations（已觀察到的視覺／備註事實）、possibleIssues（可能原因，語氣保持「可能」）、recommendations（可執行建議作法）。
                            【實際環境】若提供實際位置／日照／遮雨／介質／水盤，必須對照物種需求寫出落差，並在 recommendations 與 wateringAdvice 給出具體修正（例如澆水節奏、見乾見濕、移到半日照），不可只寫「可能與水分或光照有關」。
                            【養分假說】若視覺或備註暗示缺素，輸出 nutrientHypotheses 陣列：每筆 { nutrient, likelihood(高|中|低), visualClues, caveat }；caveat 必須含「單憑照片無法確診」；不可寫成確診。
                            【施肥】fertilizerAdvice 僅當可能原因與養分／施肥相關時才輸出 { type, npkHint, dilution, dilutionStrong, dilutionMild, frequency, notes }；與本次症狀無關則省略或 null，禁止每次塞平衡肥罐頭。
                            只能根據提供的資料判斷，不確定時必須說不確定，不可直接宣稱植物得病或確診缺素。
                            若引用外部來源，請在 citations 陣列中標記 sourceTitle、sourceUrl、reliabilityLevel、usedFor。
                            優先參考 reliabilityLevel 較低（數字越小越可靠）的來源。
                            若使用者提供照片，請仔細觀察葉片、莖部、顏色、斑點、蟲害等視覺特徵，並結合使用者備註分析。
                            輸出 JSON 欄位：summary, healthScore, observations, possibleIssues, nutrientHypotheses, environmentAssessment, recommendations, warning, citations, growthTrend, wateringAdvice, pestRisk, alerts, confidence, needsHumanReview, fertilizerAdvice(條件)。
                            【型別強制】observations、possibleIssues、recommendations、warning、alerts 必須是字串陣列（string[]），即使只有一條也要用 ["…"]，不可回傳單一字串。
                            nutrientHypotheses 必須是物件陣列（可為 []）。
                            environmentAssessment 必須是物件 { light, water, humidity, temperature }（各為字串），不可回傳單一字串；light／water 應寫評估與建議方向。
                            healthScore 為 0–100 整數；confidence 為 0–1 小數（不可用百分比字串）；needsHumanReview 為布林值。
                            """
                    },
                    userMessage
                ],
                cancellationToken: cancellationToken);

            try
            {
                var result = JsonHelper.Deserialize<PlantAnalysisResultDto>(content)
                    ?? CreatePlaceholderResult(context);
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Plant analysis JSON parse failed. Content: {Content}",
                    content is { Length: > 800 } ? content[..800] + "…" : content);
                throw;
            }
        }
        catch (ChatCompletionException ex) when (ex.Message.Contains("ApiKey", StringComparison.Ordinal))
        {
            _logger.LogWarning("OpenAI API key is not configured. Returning placeholder analysis.");
            return CreatePlaceholderResult(context);
        }
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

    private static string BuildPrompt(PlantAnalysisContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("請全程以繁體中文（台灣用字）回覆分析結果，不要使用簡體中文。");
        sb.AppendLine("請以診斷為主：已觀察／可能原因／可執行建議；水分與光照修正必須對照實際環境寫具體作法。");
        sb.AppendLine("僅當症狀可能與養分／施肥有關時才輸出 fertilizerAdvice；否則省略。");
        sb.AppendLine($"植物：{context.Plant.Name}");
        sb.AppendLine($"位置：{context.Plant.Location}");
        sb.AppendLine($"分析範圍：{context.Scope}");

        if (context.FocusImageId.HasValue)
        {
            sb.AppendLine("【本次分析重點：使用者上傳的照片與備註】");
            sb.AppendLine($"- 照片 ID：{context.FocusImageId}");
            sb.AppendLine($"- 使用者備註：{context.FocusImageNote ?? "（無）"}");
            sb.AppendLine("請依照片視覺特徵與備註做診斷：");
            sb.AppendLine("- observations：具體看到什麼（顏色、斑點、枯黑位置、新葉舊葉等）");
            sb.AppendLine("- possibleIssues：可能原因（水分過多／過少、光照過強／不足、介質等），勿只寫含糊一句");
            sb.AppendLine("- nutrientHypotheses：若像缺素，列出可能元素與視覺線索，並強調無法確診");
            sb.AppendLine("- recommendations：含水份怎麼改、光照怎麼改的具體步驟（對照下方實際環境）");
            sb.AppendLine("- wateringAdvice：一句可執行的澆水修正");
        }

        if (context.Knowledge != null)
        {
            sb.AppendLine("植物知識：");
            sb.AppendLine($"- 光照：{context.Knowledge.LightRequirement}");
            sb.AppendLine($"- 澆水：{context.Knowledge.WaterRequirement}");
            sb.AppendLine($"- 施肥：{context.Knowledge.FertilizerRequirement}");
            sb.AppendLine($"- 摘要：{context.Knowledge.CareSummary}");
        }

        sb.AppendLine("最近日記：");
        foreach (var diary in context.Diaries)
        {
            sb.AppendLine($"- {diary.DiaryDate:yyyy-MM-dd}: {diary.Title} {diary.Note}");
        }

        sb.AppendLine($"最近照片數量：{context.Images.Count}");

        if (context.PreviousAnalyses.Count > 0)
        {
            sb.AppendLine("歷史 AI 分析：");
            foreach (var analysis in context.PreviousAnalyses.Take(5))
            {
                sb.AppendLine($"- {analysis.CreateDate:yyyy-MM-dd}: 健康度 {analysis.HealthScore}，{analysis.Summary}");
            }
        }

        if (context.Sources.Count > 0)
        {
            sb.AppendLine("外部可信資料：");
            foreach (var source in context.Sources.OrderBy(s => s.ReliabilityLevel))
            {
                sb.AppendLine($"- [{source.ReliabilityLevel}] {source.SourceTitle} ({source.SourceUrl})");
                if (!string.IsNullOrWhiteSpace(source.Summary))
                {
                    sb.AppendLine($"  摘要：{source.Summary}");
                }
                if (!string.IsNullOrWhiteSpace(source.CleanText))
                {
                    var excerpt = source.CleanText.Length > 500 ? source.CleanText[..500] + "…" : source.CleanText;
                    sb.AppendLine($"  內容：{excerpt}");
                }
            }
        }

        if (context.Profile != null)
        {
            sb.AppendLine("【使用者實際環境】（分析時必須對照，不可忽略）");
            sb.AppendLine($"- 實際位置類型：{(context.Profile.ActualPlacement.HasValue ? PlacementTypeDisplay.ToLabel(context.Profile.ActualPlacement) : "未設定")}");
            sb.AppendLine($"- 實際日照：{(context.Profile.ActualLight.HasValue ? LightLevelDisplay.ToLabel(context.Profile.ActualLight) : "未設定")}");
            sb.AppendLine($"- 遮雨：{(context.Profile.HasRainCover == true ? "有遮雨" : context.Profile.HasRainCover == false ? "無遮雨" : "未設定")}");
            sb.AppendLine($"- 介質：{context.Profile.SubstrateType ?? "未設定"}");
            sb.AppendLine($"- 水盤狀態：{(context.Profile.SaucerState.HasValue ? SaucerStateDisplay.ToLabel(context.Profile.SaucerState) : "未設定")}");
            sb.AppendLine($"- 縣市：{context.Profile.City ?? "未設定"}");
            if (!string.IsNullOrWhiteSpace(context.Profile.AiEnvironmentAdvice))
            {
                sb.AppendLine($"- 既有環境適配建議：{context.Profile.AiEnvironmentAdvice}");
            }
            sb.AppendLine("個人化植物設定：");
            if (context.Profile.WateringIntervalDays.HasValue)
            {
                sb.AppendLine($"- 澆水週期：每 {context.Profile.WateringIntervalDays} 天");
            }
            if (context.Profile.FertilizingIntervalDays.HasValue)
            {
                sb.AppendLine($"- 施肥週期：每 {context.Profile.FertilizingIntervalDays} 天");
            }
            if (context.Profile.TargetHumidityMin.HasValue || context.Profile.TargetHumidityMax.HasValue)
            {
                sb.AppendLine($"- 目標濕度：{context.Profile.TargetHumidityMin}~{context.Profile.TargetHumidityMax}%");
            }
            if (context.Profile.TargetTemperatureMin.HasValue || context.Profile.TargetTemperatureMax.HasValue)
            {
                sb.AppendLine($"- 目標溫度：{context.Profile.TargetTemperatureMin}~{context.Profile.TargetTemperatureMax}°C");
            }
            if (!string.IsNullOrWhiteSpace(context.Profile.PersonalCareNotes))
            {
                sb.AppendLine($"- 備註：{context.Profile.PersonalCareNotes}");
            }
        }

        if (context.CareRecords.Count > 0)
        {
            sb.AppendLine("環境與照護紀錄：");
            foreach (var record in context.CareRecords.OrderByDescending(r => r.RecordDate).Take(20))
            {
                var value = record.NumericValue.HasValue ? $"{record.NumericValue}{record.Unit}" : record.Note;
                sb.AppendLine($"- {record.RecordDate:yyyy-MM-dd} [{record.CareType}] {value}");
            }
        }

        if (context.PreviousAnalyses.Count > 1)
        {
            var scores = context.PreviousAnalyses.Where(a => a.HealthScore.HasValue).Select(a => a.HealthScore!.Value).ToList();
            if (scores.Count >= 2)
            {
                var delta = scores.First() - scores.Last();
                sb.AppendLine($"健康度趨勢：最近 {scores.First()}，較早期 {scores.Last()}，變化 {delta:+0;-0;0}。");
            }
        }

        return sb.ToString();
    }

    private static PlantAnalysisResultDto CreatePlaceholderResult(PlantAnalysisContext context)
    {
        var citations = context.Sources
            .Take(3)
            .Select(s => new PlantCitationResultDto
            {
                SourceTitle = s.SourceTitle ?? "外部來源",
                SourceUrl = s.SourceUrl ?? string.Empty,
                ReliabilityLevel = s.ReliabilityLevel,
                UsedFor = "資料已納入分析上下文"
            })
            .ToList();

        return new PlantAnalysisResultDto
        {
            Summary = $"目前 {context.Plant.Name} 的資料已收集完成。請在設定檔或 User Secrets 中設定 AI.OpenAI.ApiKey 以啟用完整 AI 分析。",
            HealthScore = 75,
            Observations = ["已收到植物基本資料、最近日記與外部來源。"],
            PossibleIssues = [],
            Recommendations = ["持續記錄日記、環境數據與澆水紀錄。", "可至 Admin 後台新增更多外部文章來源。"],
            GrowthTrend = context.PreviousAnalyses.Count >= 2 ? "資料不足，待更多分析後可判斷。" : "尚無足夠歷史分析。",
            WateringAdvice = "請依 PlantKnowledgeModel 與最近澆水紀錄自行調整。",
            PestRisk = "資料不足，請持續觀察葉片與莖部。",
            Alerts = [],
            Citations = citations,
            Confidence = 0.5m,
            NeedsHumanReview = true,
            EnvironmentAssessment = new PlantEnvironmentAssessmentDto
            {
                Light = context.Knowledge?.LightRequirement ?? "資料不足",
                Water = context.Knowledge?.WaterRequirement ?? "資料不足",
                Humidity = context.Knowledge?.HumidityRequirement ?? "資料不足",
                Temperature = "資料不足"
            }
        };
    }
}

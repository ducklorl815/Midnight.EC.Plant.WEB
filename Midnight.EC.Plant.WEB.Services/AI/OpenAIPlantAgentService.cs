using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.AI;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Services.Configuration;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Utility.Json;

namespace Midnight.EC.Plant.WEB.Services.AI;

public class OpenAIPlantAgentService : IAIAgentService
{
    public const string PromptVersion = "plant-analysis-v5";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAIPlantAgentService> _logger;

    public OpenAIPlantAgentService(
        IHttpClientFactory httpClientFactory,
        IOptions<AiOptions> options,
        ILogger<OpenAIPlantAgentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PlantAnalysisResultDto> AnalyzePlantAsync(PlantAnalysisContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey))
        {
            _logger.LogWarning("OpenAI API key is not configured. Returning placeholder analysis.");
            return CreatePlaceholderResult(context);
        }

        var client = _httpClientFactory.CreateClient("OpenAI");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAI.ApiKey);

        var prompt = BuildPrompt(context);
        object userMessage;
        if (!string.IsNullOrWhiteSpace(context.FocusImageAbsolutePath) && File.Exists(context.FocusImageAbsolutePath))
        {
            var bytes = await File.ReadAllBytesAsync(context.FocusImageAbsolutePath, cancellationToken);
            var base64 = Convert.ToBase64String(bytes);
            var mime = context.FocusImageContentType ?? "image/jpeg";
            userMessage = new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text", text = prompt },
                    new { type = "image_url", image_url = new { url = $"data:{mime};base64,{base64}" } }
                }
            };
        }
        else
        {
            userMessage = new { role = "user", content = prompt };
        }

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
                        你是植栽照護助理，服務對象為台灣／繁體中文使用者。
                        【語言強制】所有給使用者閱讀的文字必須使用繁體中文（台灣用字），禁止簡體中文；學名、URL、數字可維持原文。
                        JSON 欄位名稱維持英文字鍵；欄位值用繁體中文。
                        【實際環境】若提供實際位置／日照／遮雨／介質，必須對照物種需求分析落差與可能問題，不可只寫百科通識。
                        【施肥】必須輸出 fertilizerAdvice：{ type, npkHint, dilution, dilutionStrong, dilutionMild, frequency, notes }；說明要可供新手製作水肥（含倍數）。
                        命名：以學名為準；若有使用者中文名可用，勿自行發明其他中文俗名。
                        只能根據提供的資料判斷，不確定時必須說不確定，不可直接宣稱植物得病。
                        必須區分 Observed / Possible / Recommended（可用繁體說明：已觀察到／可能原因／建議作法）。
                        若引用外部來源，請在 citations 陣列中標記 sourceTitle、sourceUrl、reliabilityLevel、usedFor。
                        優先參考 reliabilityLevel 較低（數字越小越可靠）的來源。
                        若使用者提供照片，請仔細觀察葉片、莖部、顏色、斑點、蟲害等視覺特徵，並結合使用者備註分析。
                        輸出 JSON 欄位：summary, healthScore, observations, possibleIssues, environmentAssessment, recommendations, warning, citations, growthTrend, wateringAdvice, pestRisk, alerts, confidence, needsHumanReview, fertilizerAdvice。
                        """
                },
                userMessage
            }
        };

        var requestJson = JsonSerializer.Serialize(payload);
        using var response = await client.PostAsync(
            "chat/completions",
            new StringContent(requestJson, Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("OpenAI analysis failed: {Error}", error);
            throw new InvalidOperationException("AI 分析失敗。");
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        var result = JsonHelper.Deserialize<PlantAnalysisResultDto>(content ?? "{}") ?? CreatePlaceholderResult(context);
        return result;
    }

    private static string BuildPrompt(PlantAnalysisContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("請全程以繁體中文（台灣用字）回覆分析結果，不要使用簡體中文。");
        sb.AppendLine("請一併給出可執行的水肥配方（fertilizerAdvice：肥種、稀釋倍數、頻率）。");
        sb.AppendLine($"植物：{context.Plant.Name}");
        sb.AppendLine($"位置：{context.Plant.Location}");
        sb.AppendLine($"分析範圍：{context.Scope}");

        if (context.FocusImageId.HasValue)
        {
            sb.AppendLine("【本次分析重點：使用者上傳的照片與備註】");
            sb.AppendLine($"- 照片 ID：{context.FocusImageId}");
            sb.AppendLine($"- 使用者備註：{context.FocusImageNote ?? "（無）"}");
            sb.AppendLine("請優先根據照片視覺特徵與備註，分析目前草況（健康、病蟲害、養分不足等可能原因）。");
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

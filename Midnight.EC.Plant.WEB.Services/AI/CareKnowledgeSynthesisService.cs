using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Services.Configuration;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.AI;

public class CareKnowledgeSynthesisService : ICareKnowledgeSynthesisService
{
    public const string PromptVersion = "care-synthesis-v5";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiOptions _options;
    private readonly ILogger<CareKnowledgeSynthesisService> _logger;

    public CareKnowledgeSynthesisService(
        IHttpClientFactory httpClientFactory,
        IOptions<AiOptions> options,
        ILogger<CareKnowledgeSynthesisService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CareSynthesisResult> SynthesizeAsync(
        string speciesKeyword,
        string? scientificName,
        ExternalKnowledgeResult mergedKnowledge,
        CancellationToken cancellationToken = default,
        bool forceRefreshGuide = false)
    {
        if (!forceRefreshGuide && !CareKnowledgeCompleteness.HasGaps(mergedKnowledge))
        {
            return CareSynthesisResult.Skipped();
        }

        if (string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey))
        {
            _logger.LogWarning("OpenAI key not configured; AI 補足 skipped.");
            return CareSynthesisResult.Failed("尚未設定 AI:OpenAI:ApiKey（請確認 Development 設定或 User Secrets）。");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("OpenAI");
            var userPrompt = BuildPrompt(speciesKeyword, scientificName, mergedKnowledge);
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
                            你是植物栽培顧問，服務對象為台灣／繁體中文使用者。
                            根據學名與外部來源，輸出 JSON。欄位名用英文；所有給使用者讀的字串值用繁體中文（台灣用字），禁止簡體。
                            temperatureMin、temperatureMax 必須是 JSON 數字。
                            必填輸出：
                            lightRequirement, suggestedLight, waterRequirement, humidityRequirement,
                            temperatureMin, temperatureMax, soilRequirement, fertilizerRequirement,
                            growthSeason, careSummary,
                            speciesGuide: {
                              chineseName, scientificName, summary,
                              fertilizer: { type, npkHint, dilution, dilutionStrong, dilutionMild, frequency, notes }
                            }
                            【命名】scientificName 填拉丁學名。chineseName 只能填「使用者輸入的中文名」；不可自行翻譯或改用其他俗名（例如使用者寫小豆樹就不可改成珍珠樹）。若使用者未提供中文名，chineseName 留空。
                            summary 請以學名起述（可附使用者中文名），不要使用未經確認的中文俗名。
                            suggestedLight 必須是下列英文枚舉之一（對應建議日照四檔）：None（無日照／陰處）、Diffuse（散射）、HalfDay（半日）、FullSun（烈日）。
                            lightRequirement 用繁中短句描述；suggestedLight 與 lightRequirement 語意一致。
                            speciesGuide.fertilizer 必須給可執行水肥建議：
                            - type：肥種（如平衡液肥）
                            - npkHint：如 20-20-20
                            - dilution：如 1000–2000 倍
                            - dilutionStrong / dilutionMild：整數倍數（如 1000 與 2000）
                            - frequency：如生長季每月 1 次
                            - notes：秋冬停肥等短提醒
                            fertilizerRequirement：把上述施肥重點收成一句繁中短句。
                            不要輸出資料來源或 API 名稱。
                            """
                    },
                    new { role = "user", content = userPrompt }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAI.ApiKey.Trim());
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var detail = Truncate(ExtractOpenAiError(responseBody) ?? responseBody, 240);
                _logger.LogWarning(
                    "Care synthesis OpenAI failed: {Status} {Detail}",
                    (int)response.StatusCode,
                    detail);
                return CareSynthesisResult.Failed($"OpenAI HTTP {(int)response.StatusCode}：{detail}");
            }

            using var document = JsonDocument.Parse(responseBody);
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            content = StripMarkdownFence(content);
            if (string.IsNullOrWhiteSpace(content))
            {
                return CareSynthesisResult.Failed("OpenAI 回傳內容為空。");
            }

            ExternalKnowledgePartial partial;
            try
            {
                partial = ParseCarePartial(content);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Care synthesis JSON parse failed. Content: {Content}", Truncate(content, 400));
                return CareSynthesisResult.Failed($"無法解析 OpenAI JSON：{Truncate(ex.Message, 160)}");
            }

            if (IsEffectivelyEmpty(partial))
            {
                return CareSynthesisResult.Failed("OpenAI 有回應，但未提供可用的照護欄位。");
            }

            return CareSynthesisResult.Succeeded(partial);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Care knowledge synthesis failed for {Keyword}", speciesKeyword);
            return CareSynthesisResult.Failed($"呼叫 OpenAI 例外：{ex.GetType().Name} — {Truncate(ex.Message, 180)}");
        }
    }

    private ExternalKnowledgePartial ParseCarePartial(string content)
    {
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        SpeciesCareGuideDto? guide = null;
        if (TryGetPropertyIgnoreCase(root, "speciesGuide", out var guideEl) &&
            guideEl.ValueKind == JsonValueKind.Object)
        {
            guide = new SpeciesCareGuideDto
            {
                ChineseName = NullIfEmpty(GetString(guideEl, "chineseName")),
                ScientificName = NullIfEmpty(GetString(guideEl, "scientificName")),
                Summary = NullIfEmpty(GetString(guideEl, "summary")),
                Fertilizer = ParseFertilizer(guideEl)
            };
        }

        // 相容舊版：externalCareGuide 純文字
        if (guide == null || (string.IsNullOrWhiteSpace(guide.Summary) && guide.Fertilizer == null))
        {
            var legacy = NullIfEmpty(GetString(root, "externalCareGuide"));
            if (!string.IsNullOrWhiteSpace(legacy) && !CareGuideJson.LooksLikeJson(legacy))
            {
                guide = new SpeciesCareGuideDto
                {
                    Summary = legacy,
                    Fertilizer = ParseFertilizer(root)
                };
            }
        }

        var fertilizerRequirement = NullIfEmpty(GetString(root, "fertilizerRequirement"));
        if (string.IsNullOrWhiteSpace(fertilizerRequirement) && guide?.Fertilizer != null)
        {
            fertilizerRequirement = NullIfEmpty(CareGuideJson.FormatFertilizerRequirement(guide.Fertilizer));
        }

        string? externalCareGuide = null;
        if (guide != null && (!string.IsNullOrWhiteSpace(guide.Summary) || guide.Fertilizer != null
            || !string.IsNullOrWhiteSpace(guide.ChineseName)))
        {
            externalCareGuide = CareGuideJson.Serialize(guide);
        }

        return new ExternalKnowledgePartial
        {
            LightRequirement = NullIfEmpty(GetString(root, "lightRequirement")),
            WaterRequirement = NullIfEmpty(GetString(root, "waterRequirement")),
            HumidityRequirement = NullIfEmpty(GetString(root, "humidityRequirement")),
            TemperatureMin = GetOptionalDecimal(root, "temperatureMin"),
            TemperatureMax = GetOptionalDecimal(root, "temperatureMax"),
            SoilRequirement = NullIfEmpty(GetString(root, "soilRequirement")),
            FertilizerRequirement = fertilizerRequirement,
            GrowthSeason = NullIfEmpty(GetString(root, "growthSeason")),
            CareSummary = NullIfEmpty(GetString(root, "careSummary")),
            ExternalCareGuide = externalCareGuide,
            SuggestedLight = ParseSuggestedLight(root),
            Provider = $"AI-{PromptVersion}"
        };
    }

    private static Midnight.EC.Plant.WEB.Models.Enums.LightLevel? ParseSuggestedLight(JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "suggestedLight", out var el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.String => Midnight.EC.Plant.WEB.Models.Enums.LightLevelDisplay.TryParseSuggestedLightToken(el.GetString()),
            JsonValueKind.Number when el.TryGetInt32(out var n)
                => Midnight.EC.Plant.WEB.Models.Enums.LightLevelDisplay.TryParseSuggestedLightToken(n.ToString()),
            _ => Midnight.EC.Plant.WEB.Models.Enums.LightLevelDisplay.TryParseSuggestedLightToken(el.ToString())
        };
    }

    private static FertilizerRecipeDto? ParseFertilizer(JsonElement parent)
    {
        if (!TryGetPropertyIgnoreCase(parent, "fertilizer", out var el) || el.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var recipe = new FertilizerRecipeDto
        {
            Type = NullIfEmpty(GetString(el, "type")),
            NpkHint = NullIfEmpty(GetString(el, "npkHint")),
            Dilution = NullIfEmpty(GetString(el, "dilution")),
            DilutionStrong = GetOptionalInt(el, "dilutionStrong"),
            DilutionMild = GetOptionalInt(el, "dilutionMild"),
            Frequency = NullIfEmpty(GetString(el, "frequency")),
            Notes = NullIfEmpty(GetString(el, "notes"))
        };

        if (recipe.DilutionStrong is null || recipe.DilutionMild is null)
        {
            var (strong, mild) = CareGuideJson.ResolveDilutionRange(recipe);
            recipe.DilutionStrong ??= strong;
            recipe.DilutionMild ??= mild;
            if (string.IsNullOrWhiteSpace(recipe.Dilution))
            {
                recipe.Dilution = $"{recipe.DilutionStrong}–{recipe.DilutionMild} 倍";
            }
        }

        if (string.IsNullOrWhiteSpace(recipe.Type)
            && string.IsNullOrWhiteSpace(recipe.Dilution)
            && string.IsNullOrWhiteSpace(recipe.Frequency))
        {
            return null;
        }

        return recipe;
    }

    public async Task<EnvironmentFitResult> SynthesizeEnvironmentFitAsync(
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
        {
            return EnvironmentFitResult.Skipped();
        }

        if (string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey))
        {
            return EnvironmentFitResult.Failed("尚未設定 AI:OpenAI:ApiKey。");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("OpenAI");
            var userPrompt = BuildEnvironmentFitPrompt(plantDisplayName, scientificName, knowledge, environment);
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
                            你是台灣居家植栽顧問。必須依「使用者實際環境」對照「物種照護需求」做適配分析，不可只寫物種百科通識。
                            【語言】全文繁體中文（台灣用字），禁止簡體。JSON 鍵名用英文。
                            若提供水盤狀態，必須在 adjustments 或 problems 中明確建議：這盆該拿掉水盤、可留盤但勿積水、或適合淺盤保濕；並簡短說明原因（接水≠蓄水延長）。
                            輸出 JSON：
                            {
                              "fitSummary": "1～2 句契合／落差",
                              "problems": [ { "text": "問題與原因", "hint": "可選附註，如建議換土" } ],
                              "adjustments": [ "可執行調整1", "調整2" ],
                              "fertilizerAdjustment": "一句話：在此環境下施肥如何調整（例如顆粒土先用較淡倍數）；不要重複完整配方教學"
                            }
                            problems 列 2～4 項；adjustments 分行條列。
                            不要重複貼一整篇物種百科。
                            """
                    },
                    new { role = "user", content = userPrompt }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAI.ApiKey.Trim());
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var detail = Truncate(ExtractOpenAiError(responseBody) ?? responseBody, 240);
                return EnvironmentFitResult.Failed($"OpenAI HTTP {(int)response.StatusCode}：{detail}");
            }

            using var document = JsonDocument.Parse(responseBody);
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            content = StripMarkdownFence(content);
            if (string.IsNullOrWhiteSpace(content))
            {
                return EnvironmentFitResult.Failed("OpenAI 回傳內容為空。");
            }

            using var adviceDoc = JsonDocument.Parse(content);
            var root = adviceDoc.RootElement;

            // 新結構
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

            // 舊版相容：advice 字串
            var advice = GetString(root, "advice");
            if (string.IsNullOrWhiteSpace(advice))
            {
                advice = content.Trim();
            }

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

    private static List<EnvironmentProblemDto> ParseProblems(JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "problems", out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<EnvironmentProblemDto>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var text = item.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    list.Add(new EnvironmentProblemDto { Text = text.Trim() });
                }
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var textVal = NullIfEmpty(GetString(item, "text")) ?? NullIfEmpty(GetString(item, "problem"));
            if (string.IsNullOrWhiteSpace(textVal))
            {
                continue;
            }

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
        {
            return [];
        }

        return el.EnumerateArray()
            .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToList();
    }

    private static string BuildEnvironmentFitPrompt(
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
            {
                sb.AppendLine($"- {w}");
            }
        }

        sb.AppendLine("【物種照護需求（外部知識）】");
        sb.AppendLine($"- 光照：{knowledge.LightRequirement}");
        sb.AppendLine($"- 澆水：{knowledge.WaterRequirement}");
        sb.AppendLine($"- 濕度：{knowledge.HumidityRequirement}");
        sb.AppendLine($"- 溫度：{knowledge.TemperatureMin}~{knowledge.TemperatureMax}°C");
        sb.AppendLine($"- 土壤：{knowledge.SoilRequirement}");
        sb.AppendLine($"- 施肥：{knowledge.FertilizerRequirement}");
        sb.AppendLine($"- 生長季：{knowledge.GrowthSeason}");
        if (!string.IsNullOrWhiteSpace(knowledge.ExternalCareGuide))
        {
            var parsed = CareGuideJson.TryParseSpeciesGuide(knowledge.ExternalCareGuide);
            if (parsed != null)
            {
                sb.AppendLine($"- 物種說明：{parsed.Summary}");
                if (parsed.Fertilizer != null)
                {
                    sb.AppendLine($"- 物種施肥配方：{CareGuideJson.FormatFertilizerRequirement(parsed.Fertilizer)}");
                }
            }
            else
            {
                var guide = knowledge.ExternalCareGuide.Length > 600
                    ? knowledge.ExternalCareGuide[..600] + "…"
                    : knowledge.ExternalCareGuide;
                sb.AppendLine($"- 物種說明摘要：{guide}");
            }
        }

        sb.AppendLine("請產出結構化適配分析（fitSummary／problems／adjustments／fertilizerAdjustment），繁體中文。");
        return sb.ToString();
    }

    private static string? GetString(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el))
        {
            return null;
        }

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

    private static decimal? GetOptionalDecimal(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Number when el.TryGetDecimal(out var n) => n,
            JsonValueKind.String => ParseDecimalLoose(el.GetString()),
            _ => ParseDecimalLoose(el.ToString())
        };
    }

    private static int? GetOptionalInt(JsonElement root, string name)
    {
        var d = GetOptionalDecimal(root, name);
        if (!d.HasValue)
        {
            return null;
        }

        return (int)Math.Round(d.Value, MidpointRounding.AwayFromZero);
    }

    private static decimal? ParseDecimalLoose(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim();
        if (text is "null" or "undefined" or "-" or "未知" or "N/A" or "n/a")
        {
            return null;
        }

        // 抽出第一個數字（允許負號與小數），例如 "10°C"、"約 18 度"、"10-28"
        var match = System.Text.RegularExpressions.Regex.Match(text, @"-?\d+(\.\d+)?");
        if (!match.Success)
        {
            return null;
        }

        return decimal.TryParse(match.Value, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string name, out JsonElement value)
    {
        if (root.TryGetProperty(name, out value))
        {
            return true;
        }

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

    private static bool IsEffectivelyEmpty(ExternalKnowledgePartial p) =>
        string.IsNullOrWhiteSpace(p.LightRequirement)
        && string.IsNullOrWhiteSpace(p.WaterRequirement)
        && string.IsNullOrWhiteSpace(p.HumidityRequirement)
        && !p.TemperatureMin.HasValue
        && !p.TemperatureMax.HasValue
        && string.IsNullOrWhiteSpace(p.SoilRequirement)
        && string.IsNullOrWhiteSpace(p.FertilizerRequirement)
        && string.IsNullOrWhiteSpace(p.GrowthSeason)
        && string.IsNullOrWhiteSpace(p.ExternalCareGuide);

    private static string BuildPrompt(string keyword, string? scientificName, ExternalKnowledgeResult merged)
    {
        var sb = new StringBuilder();
        sb.AppendLine("請以繁體中文（台灣用字）填寫下列缺口；不要使用簡體中文。");
        sb.AppendLine($"使用者輸入（中文名以此為準，禁止改成其他俗名）：{keyword}");
        sb.AppendLine($"學名：{scientificName ?? "未知"}");
        sb.AppendLine("已有外部資料（空值請優先補齊）：");
        sb.AppendLine($"- 光照：{merged.LightRequirement}");
        sb.AppendLine($"- 澆水：{merged.WaterRequirement}");
        sb.AppendLine($"- 濕度：{merged.HumidityRequirement}");
        sb.AppendLine($"- 溫度：{merged.TemperatureMin}~{merged.TemperatureMax}°C");
        sb.AppendLine($"- 介質：{merged.SoilRequirement}");
        sb.AppendLine($"- 施肥：{merged.FertilizerRequirement}");
        sb.AppendLine($"- 生長季：{merged.GrowthSeason}");
        sb.AppendLine($"- 外部照護指南：{merged.ExternalCareGuide}");
        sb.AppendLine($"- 摘要：{merged.CareSummary}");
        sb.AppendLine("請補齊缺失的照護欄位，並產出 speciesGuide（含施肥配方 type／dilution／frequency）；全文繁體中文。");
        sb.AppendLine("suggestedLight 必填：None / Diffuse / HalfDay / FullSun 其一。");
        sb.AppendLine("temperatureMin / temperatureMax 必須是純數字（例如 10 或 28），不可寫字串或單位。");
        return sb.ToString();
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

    private static string? ExtractOpenAiError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                {
                    return message.GetString();
                }

                return error.ToString();
            }
        }
        catch
        {
            // ignore parse errors
        }

        return null;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Services.Configuration;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Utility.Json;

namespace Midnight.EC.Plant.WEB.Services.AI;

public class CareKnowledgeSynthesisService : ICareKnowledgeSynthesisService
{
    public const string PromptVersion = "care-synthesis-v1";

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

    public async Task<ExternalKnowledgePartial?> SynthesizeAsync(
        string speciesKeyword,
        string? scientificName,
        ExternalKnowledgeResult mergedKnowledge,
        CancellationToken cancellationToken = default)
    {
        if (IsComplete(mergedKnowledge))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey))
        {
            _logger.LogDebug("OpenAI key not configured; skip care synthesis.");
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAI.ApiKey);

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
                            你是植物栽培顧問。根據提供的學名與外部來源摘要，輸出 JSON 照護建議（繁體中文）。
                            只能填寫有合理依據的欄位；不確定可留空字串或 null。
                            輸出欄位：lightRequirement, waterRequirement, humidityRequirement,
                            temperatureMin, temperatureMax, soilRequirement, fertilizerRequirement,
                            growthSeason, careSummary, externalCareGuide。
                            externalCareGuide 只需照護要點敘述，不要包含資料來源、學名或 API 名稱。
                            careSummary 可留空。
                            """
                    },
                    new { role = "user", content = userPrompt }
                }
            };

            using var response = await client.PostAsync(
                "chat/completions",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Care synthesis OpenAI failed: {Status}", response.StatusCode);
                return null;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var dto = JsonHelper.Deserialize<CareSynthesisDto>(content ?? "{}");
            if (dto == null)
            {
                return null;
            }

            return new ExternalKnowledgePartial
            {
                LightRequirement = NullIfEmpty(dto.LightRequirement),
                WaterRequirement = NullIfEmpty(dto.WaterRequirement),
                HumidityRequirement = NullIfEmpty(dto.HumidityRequirement),
                TemperatureMin = dto.TemperatureMin,
                TemperatureMax = dto.TemperatureMax,
                SoilRequirement = NullIfEmpty(dto.SoilRequirement),
                FertilizerRequirement = NullIfEmpty(dto.FertilizerRequirement),
                GrowthSeason = NullIfEmpty(dto.GrowthSeason),
                CareSummary = NullIfEmpty(dto.CareSummary),
                ExternalCareGuide = NullIfEmpty(dto.ExternalCareGuide),
                Provider = $"AI-{PromptVersion}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Care knowledge synthesis failed for {Keyword}", speciesKeyword);
            return null;
        }
    }

    private static bool IsComplete(ExternalKnowledgeResult k) =>
        !string.IsNullOrWhiteSpace(k.LightRequirement)
        && !string.IsNullOrWhiteSpace(k.WaterRequirement)
        && !string.IsNullOrWhiteSpace(k.HumidityRequirement)
        && k.TemperatureMin.HasValue
        && k.TemperatureMax.HasValue;

    private static string BuildPrompt(string keyword, string? scientificName, ExternalKnowledgeResult merged)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"使用者輸入：{keyword}");
        sb.AppendLine($"學名：{scientificName ?? "未知"}");
        sb.AppendLine("已有外部資料：");
        sb.AppendLine($"- 光照：{merged.LightRequirement}");
        sb.AppendLine($"- 澆水：{merged.WaterRequirement}");
        sb.AppendLine($"- 濕度：{merged.HumidityRequirement}");
        sb.AppendLine($"- 溫度：{merged.TemperatureMin}~{merged.TemperatureMax}°C");
        sb.AppendLine($"- 介質：{merged.SoilRequirement}");
        sb.AppendLine($"- 施肥：{merged.FertilizerRequirement}");
        sb.AppendLine($"- 摘要：{merged.CareSummary}");
        sb.AppendLine("請補齊缺失的照護欄位。");
        return sb.ToString();
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class CareSynthesisDto
    {
        public string? LightRequirement { get; set; }
        public string? WaterRequirement { get; set; }
        public string? HumidityRequirement { get; set; }
        public decimal? TemperatureMin { get; set; }
        public decimal? TemperatureMax { get; set; }
        public string? SoilRequirement { get; set; }
        public string? FertilizerRequirement { get; set; }
        public string? GrowthSeason { get; set; }
        public string? CareSummary { get; set; }
        public string? ExternalCareGuide { get; set; }
    }
}

using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.External;

/// <summary>
/// 外部同步後觸發 AI 補足的欄位缺口判定（不含 CareSummary）。
/// </summary>
public static class CareKnowledgeCompleteness
{
    public static bool HasGaps(ExternalKnowledgeResult knowledge) =>
        ListMissingFields(knowledge).Count > 0;

    public static bool HasGaps(PlantKnowledgeModel knowledge) =>
        ListMissingFields(knowledge).Count > 0;

    public static bool HasGaps(Midnight.EC.Plant.WEB.Models.DTOs.PlantKnowledgeDto knowledge) =>
        ListMissingFields(knowledge).Count > 0;

    public static IReadOnlyList<string> ListMissingFields(ExternalKnowledgeResult knowledge)
    {
        var missing = new List<string>();
        AddIfBlank(missing, "光照", knowledge.LightRequirement);
        if (!ResolveSuggestedLight(knowledge.SuggestedLight, knowledge.LightRequirement, knowledge.CareSummary, knowledge.ExternalCareGuide).HasValue)
        {
            missing.Add("建議日照");
        }

        AddIfBlank(missing, "澆水", knowledge.WaterRequirement);
        AddIfBlank(missing, "濕度", knowledge.HumidityRequirement);
        if (!knowledge.TemperatureMin.HasValue)
        {
            missing.Add("溫度下限");
        }

        if (!knowledge.TemperatureMax.HasValue)
        {
            missing.Add("溫度上限");
        }

        AddIfBlank(missing, "土壤", knowledge.SoilRequirement);
        AddIfBlank(missing, "施肥", knowledge.FertilizerRequirement);
        AddIfBlank(missing, "生長季", knowledge.GrowthSeason);
        if (IsBlank(knowledge.ExternalCareGuide) || CareGuideJson.TryParseSpeciesGuide(knowledge.ExternalCareGuide) == null)
        {
            missing.Add("結構化照護指南");
        }

        return missing;
    }

    public static IReadOnlyList<string> ListMissingFields(PlantKnowledgeModel knowledge) =>
        ListMissingFields(
            knowledge.SuggestedLight,
            knowledge.LightRequirement,
            knowledge.WaterRequirement,
            knowledge.HumidityRequirement,
            knowledge.TemperatureMin,
            knowledge.TemperatureMax,
            knowledge.SoilRequirement,
            knowledge.FertilizerRequirement,
            knowledge.GrowthSeason,
            knowledge.CareSummary,
            knowledge.ExternalCareGuide);

    public static IReadOnlyList<string> ListMissingFields(Midnight.EC.Plant.WEB.Models.DTOs.PlantKnowledgeDto knowledge) =>
        ListMissingFields(
            knowledge.SuggestedLight,
            knowledge.LightRequirement,
            knowledge.WaterRequirement,
            knowledge.HumidityRequirement,
            knowledge.TemperatureMin,
            knowledge.TemperatureMax,
            knowledge.SoilRequirement,
            knowledge.FertilizerRequirement,
            knowledge.GrowthSeason,
            knowledge.CareSummary,
            knowledge.ExternalCareGuide);

    public static LightLevel? ResolveSuggestedLight(
        LightLevel? stored,
        string? lightRequirement,
        string? careSummary = null,
        string? externalCareGuide = null) =>
        stored
        ?? LightLevelDisplay.TryParseFromText(lightRequirement)
        ?? LightLevelDisplay.TryParseFromText(careSummary)
        ?? LightLevelDisplay.TryParseFromText(externalCareGuide);

    private static IReadOnlyList<string> ListMissingFields(
        LightLevel? suggestedLight,
        string? lightRequirement,
        string? waterRequirement,
        string? humidityRequirement,
        decimal? temperatureMin,
        decimal? temperatureMax,
        string? soilRequirement,
        string? fertilizerRequirement,
        string? growthSeason,
        string? careSummary,
        string? externalCareGuide)
    {
        var missing = new List<string>();
        AddIfBlank(missing, "光照", lightRequirement);
        if (!ResolveSuggestedLight(suggestedLight, lightRequirement, careSummary, externalCareGuide).HasValue)
        {
            missing.Add("建議日照");
        }

        AddIfBlank(missing, "澆水", waterRequirement);
        AddIfBlank(missing, "濕度", humidityRequirement);
        if (!temperatureMin.HasValue)
        {
            missing.Add("溫度下限");
        }

        if (!temperatureMax.HasValue)
        {
            missing.Add("溫度上限");
        }

        AddIfBlank(missing, "土壤", soilRequirement);
        AddIfBlank(missing, "施肥", fertilizerRequirement);
        AddIfBlank(missing, "生長季", growthSeason);
        if (IsBlank(externalCareGuide) || CareGuideJson.TryParseSpeciesGuide(externalCareGuide) == null)
        {
            missing.Add("結構化照護指南");
        }

        return missing;
    }

    private static void AddIfBlank(List<string> missing, string label, string? value)
    {
        if (IsBlank(value))
        {
            missing.Add(label);
        }
    }

    private static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);
}

public enum CareSynthesisAttemptStatus
{
    SkippedAlreadyComplete = 0,
    Succeeded = 1,
    ServiceFailed = 2
}

public class CareSynthesisResult
{
    public CareSynthesisAttemptStatus Status { get; init; }
    public ExternalKnowledgePartial? Partial { get; init; }
    public string? FailureReason { get; init; }

    public static CareSynthesisResult Skipped() => new()
    {
        Status = CareSynthesisAttemptStatus.SkippedAlreadyComplete
    };

    public static CareSynthesisResult Succeeded(ExternalKnowledgePartial partial) => new()
    {
        Status = CareSynthesisAttemptStatus.Succeeded,
        Partial = partial
    };

    public static CareSynthesisResult Failed(string reason) => new()
    {
        Status = CareSynthesisAttemptStatus.ServiceFailed,
        FailureReason = reason
    };
}

public enum AiSupplementOutcome
{
    NotNeeded = 0,
    Applied = 1,
    AppliedWithRemainingGaps = 2,
    ServiceFailed = 3
}

public class KnowledgeRefreshResult
{
    public required Midnight.EC.Plant.WEB.Models.DTOs.PlantKnowledgeDto Knowledge { get; init; }
    public AiSupplementOutcome AiSupplement { get; init; }
    public string? AiFailureReason { get; init; }
    public bool EnvironmentAdviceUpdated { get; init; }
    public string? EnvironmentAdviceFailureReason { get; init; }
}

public class PlantEnvironmentContext
{
    public string? PlacementLabel { get; set; }
    public string? LightLabel { get; set; }
    public string? RainCoverLabel { get; set; }
    public string? SubstrateType { get; set; }
    public string? SaucerLabel { get; set; }
    public string? City { get; set; }
    public IReadOnlyList<string> MismatchWarnings { get; set; } = [];
}

public class EnvironmentFitResult
{
    public bool Succeeded { get; init; }
    public string? Advice { get; init; }
    public string? FailureReason { get; init; }
    public bool SkippedNoEnvironment { get; init; }

    public static EnvironmentFitResult Skipped() => new() { SkippedNoEnvironment = true };
    public static EnvironmentFitResult Ok(string advice) => new() { Succeeded = true, Advice = advice };
    public static EnvironmentFitResult Failed(string reason) => new() { FailureReason = reason };
}

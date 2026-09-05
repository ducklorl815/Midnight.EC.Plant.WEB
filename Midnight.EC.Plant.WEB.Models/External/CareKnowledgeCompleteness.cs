using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.External;

/// <summary>
/// 外部同步後觸發 AI 補足的欄位缺口判定（不含 CareSummary）。
/// </summary>
public static class CareKnowledgeCompleteness
{
    public static bool HasGaps(ExternalKnowledgeResult knowledge) =>
        IsBlank(knowledge.LightRequirement)
        || IsBlank(knowledge.WaterRequirement)
        || IsBlank(knowledge.HumidityRequirement)
        || !knowledge.TemperatureMin.HasValue
        || !knowledge.TemperatureMax.HasValue
        || IsBlank(knowledge.SoilRequirement)
        || IsBlank(knowledge.FertilizerRequirement)
        || IsBlank(knowledge.GrowthSeason)
        || IsBlank(knowledge.ExternalCareGuide)
        || CareGuideJson.TryParseSpeciesGuide(knowledge.ExternalCareGuide) == null;

    public static bool HasGaps(PlantKnowledgeModel knowledge) =>
        IsBlank(knowledge.LightRequirement)
        || IsBlank(knowledge.WaterRequirement)
        || IsBlank(knowledge.HumidityRequirement)
        || !knowledge.TemperatureMin.HasValue
        || !knowledge.TemperatureMax.HasValue
        || IsBlank(knowledge.SoilRequirement)
        || IsBlank(knowledge.FertilizerRequirement)
        || IsBlank(knowledge.GrowthSeason)
        || IsBlank(knowledge.ExternalCareGuide)
        || CareGuideJson.TryParseSpeciesGuide(knowledge.ExternalCareGuide) == null;

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

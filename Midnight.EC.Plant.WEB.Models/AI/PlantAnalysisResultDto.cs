using System.Text.Json.Serialization;
using Midnight.EC.Plant.WEB.Models.External;

namespace Midnight.EC.Plant.WEB.Models.AI;

[JsonConverter(typeof(PlantAnalysisResultJsonConverter))]
public class PlantAnalysisResultDto
{
    public string Summary { get; set; } = string.Empty;
    public int HealthScore { get; set; }
    public List<string> Observations { get; set; } = [];
    public List<string> PossibleIssues { get; set; } = [];
    public List<NutrientHypothesisDto> NutrientHypotheses { get; set; } = [];
    public PlantEnvironmentAssessmentDto EnvironmentAssessment { get; set; } = new();
    public List<string> Recommendations { get; set; } = [];
    public List<string> Warning { get; set; } = [];
    public List<PlantCitationResultDto> Citations { get; set; } = [];
    public string? GrowthTrend { get; set; }
    public string? WateringAdvice { get; set; }
    public string? PestRisk { get; set; }
    public List<string> Alerts { get; set; } = [];
    public decimal Confidence { get; set; }
    public bool NeedsHumanReview { get; set; }
    /// <summary>僅當本次症狀與養分／施肥相關時才輸出。</summary>
    public FertilizerRecipeDto? FertilizerAdvice { get; set; }
}

public class NutrientHypothesisDto
{
    /// <summary>元素或養分名稱，例如鐵、鎂、氮。</summary>
    public string Nutrient { get; set; } = string.Empty;
    /// <summary>可能性：高／中／低。</summary>
    public string Likelihood { get; set; } = string.Empty;
    /// <summary>對應的視覺線索。</summary>
    public string? VisualClues { get; set; }
    /// <summary>但書，應含「單憑照片無法確診」。</summary>
    public string Caveat { get; set; } = "單憑照片無法確診。";
}

public class PlantCitationResultDto
{
    public string SourceTitle { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public int ReliabilityLevel { get; set; }
    public string? UsedFor { get; set; }
}

[JsonConverter(typeof(PlantEnvironmentAssessmentJsonConverter))]
public class PlantEnvironmentAssessmentDto
{
    public string Light { get; set; } = string.Empty;
    public string Water { get; set; } = string.Empty;
    public string Humidity { get; set; } = string.Empty;
    public string Temperature { get; set; } = string.Empty;
}

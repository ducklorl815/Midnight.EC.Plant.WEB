namespace Midnight.EC.Plant.WEB.Models.AI;

public class PlantAnalysisResultDto
{
    public string Summary { get; set; } = string.Empty;
    public int HealthScore { get; set; }
    public List<string> Observations { get; set; } = [];
    public List<string> PossibleIssues { get; set; } = [];
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
}

public class PlantCitationResultDto
{
    public string SourceTitle { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public int ReliabilityLevel { get; set; }
    public string? UsedFor { get; set; }
}

public class PlantEnvironmentAssessmentDto
{
    public string Light { get; set; } = string.Empty;
    public string Water { get; set; } = string.Empty;
    public string Humidity { get; set; } = string.Empty;
    public string Temperature { get; set; } = string.Empty;
}

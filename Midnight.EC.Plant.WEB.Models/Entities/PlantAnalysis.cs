using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Entities;

public class PlantAnalysis
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public int? DiaryId { get; set; }
    public int? ImageId { get; set; }
    public AnalysisType AnalysisType { get; set; } = AnalysisType.General;
    public AnalysisScope AnalysisScope { get; set; } = AnalysisScope.Recent30Days;
    public string? ModelName { get; set; }
    public string? PromptVersion { get; set; }
    public string? InputSnapshot { get; set; }
    public string? ResultJson { get; set; }
    public string? Summary { get; set; }
    public int? HealthScore { get; set; }
    public decimal? Confidence { get; set; }
    public DateTime CreatedAt { get; set; }

    public Plant Plant { get; set; } = null!;
    public PlantDiary? Diary { get; set; }
    public PlantAnalysisJob? Job { get; set; }
}

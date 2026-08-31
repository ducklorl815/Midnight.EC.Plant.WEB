using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantAnalysisDto
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public int? DiaryId { get; set; }
    public int? ImageId { get; set; }
    public AnalysisType AnalysisType { get; set; }
    public AnalysisScope AnalysisScope { get; set; }
    public string? ModelName { get; set; }
    public string? PromptVersion { get; set; }
    public string? Summary { get; set; }
    public int? HealthScore { get; set; }
    public decimal? Confidence { get; set; }
    public string? ResultJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantAnalysisDto
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public Guid? DiaryId { get; set; }
    public Guid? ImageId { get; set; }
    public AnalysisType AnalysisType { get; set; }
    public AnalysisScope AnalysisScope { get; set; }
    public string? ModelName { get; set; }
    public string? PromptVersion { get; set; }
    public string? Summary { get; set; }
    public int? HealthScore { get; set; }
    public decimal? Confidence { get; set; }
    public string? ResultJson { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime CreatedAt { get => CreateDate; set => CreateDate = value; }
}

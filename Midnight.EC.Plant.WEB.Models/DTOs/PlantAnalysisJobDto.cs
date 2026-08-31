using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantAnalysisJobDto
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public int? DiaryId { get; set; }
    public int? ImageId { get; set; }
    public int? AnalysisId { get; set; }
    public AnalysisJobStatus Status { get; set; }
    public AnalysisScope AnalysisScope { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

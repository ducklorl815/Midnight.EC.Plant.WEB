using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantAnalysisJobDto
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public Guid? DiaryId { get; set; }
    public Guid? ImageId { get; set; }
    public Guid? AnalysisId { get; set; }
    public AnalysisJobStatus Status { get; set; }
    public AnalysisScope AnalysisScope { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime CreatedAt { get => CreateDate; set => CreateDate = value; }
    public DateTime? CompletedAt { get; set; }
}

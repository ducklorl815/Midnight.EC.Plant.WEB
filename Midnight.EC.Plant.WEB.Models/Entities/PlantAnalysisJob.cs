using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Entities;

public class PlantAnalysisJob
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public int? DiaryId { get; set; }
    public int? ImageId { get; set; }
    public int? AnalysisId { get; set; }
    public AnalysisJobStatus Status { get; set; } = AnalysisJobStatus.Pending;
    public AnalysisScope AnalysisScope { get; set; } = AnalysisScope.Recent30Days;
    public int RetryCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }

    public Plant Plant { get; set; } = null!;
    public PlantDiary? Diary { get; set; }
    public PlantAnalysis? Analysis { get; set; }
}

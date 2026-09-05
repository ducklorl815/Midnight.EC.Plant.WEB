using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantAnalysisJobModel
{
    public Guid ID { get; set; }
    public Guid Id { get => ID; set => ID = value; }
    public int Seq { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime CreatedAt { get => CreateDate; set => CreateDate = value; }
    public DateTime ModifyDate { get; set; }
    public DateTime UpdatedAt { get => ModifyDate; set => ModifyDate = value; }
    public bool Enabled { get; set; } = true;
    public bool IsActive { get => Enabled; set => Enabled = value; }
    public bool Deleted { get; set; }
    public Guid PlantID { get; set; }
    public Guid PlantId { get => PlantID; set => PlantID = value; }
    public Guid? DiaryID { get; set; }
    public Guid? DiaryId { get => DiaryID; set => DiaryID = value; }
    public Guid? ImageID { get; set; }
    public Guid? ImageId { get => ImageID; set => ImageID = value; }
    public Guid? AnalysisID { get; set; }
    public Guid? AnalysisId { get => AnalysisID; set => AnalysisID = value; }
    public AnalysisJobStatus Status { get; set; } = AnalysisJobStatus.Pending;
    public AnalysisScope AnalysisScope { get; set; } = AnalysisScope.Recent30Days;
    public int RetryCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

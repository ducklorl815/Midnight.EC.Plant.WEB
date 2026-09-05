using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantAnalysisModel
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
    public AnalysisType AnalysisType { get; set; } = AnalysisType.General;
    public AnalysisScope AnalysisScope { get; set; } = AnalysisScope.Recent30Days;
    public string? ModelName { get; set; }
    public string? PromptVersion { get; set; }
    public string? InputSnapshot { get; set; }
    public string? ResultJson { get; set; }
    public string? Summary { get; set; }
    public int? HealthScore { get; set; }
    public decimal? Confidence { get; set; }
}

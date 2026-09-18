using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantCareRecordModel
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
    public DateTime RecordDate { get; set; }
    public CareRecordType CareType { get; set; }
    public Guid? FertilizerProductID { get; set; }
    public Guid? FertilizerProductId { get => FertilizerProductID; set => FertilizerProductID = value; }
    public decimal? NumericValue { get; set; }
    public string? Unit { get; set; }
    public string? Note { get; set; }
}

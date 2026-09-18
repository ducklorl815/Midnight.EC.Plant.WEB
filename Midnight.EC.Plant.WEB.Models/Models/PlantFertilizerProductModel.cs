namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantFertilizerProductModel
{
    public Guid ID { get; set; }
    public Guid Id { get => ID; set => ID = value; }
    public int Seq { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime ModifyDate { get; set; }
    public bool Enabled { get; set; } = true;
    public bool Deleted { get; set; }
    public Guid PlantID { get; set; }
    public Guid PlantId { get => PlantID; set => PlantID = value; }
    public string Name { get; set; } = string.Empty;
    public int IntervalDays { get; set; }
    public int SortOrder { get; set; }
}

namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantSourceSpeciesModel
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
    public Guid SourceID { get; set; }
    public Guid SourceId { get => SourceID; set => SourceID = value; }
    public Guid SpeciesID { get; set; }
    public Guid SpeciesId { get => SpeciesID; set => SpeciesID = value; }
}

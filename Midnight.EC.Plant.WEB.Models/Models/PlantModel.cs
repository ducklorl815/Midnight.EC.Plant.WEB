namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantModel
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
    public string Name { get; set; } = string.Empty;
    public Guid SpeciesID { get; set; }
    public Guid SpeciesId { get => SpeciesID; set => SpeciesID = value; }
    public string? NickName { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? EnvironmentNote { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? StartDate { get; set; }

    public PlantSpeciesModel? Species { get; set; }
    public PlantProfileModel? Profile { get; set; }
}

namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantSpeciesModel
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
    public string ScientificName { get; set; } = string.Empty;
    public string? CommonName { get; set; }
    public string? ChineseName { get; set; }
    public string? Genus { get; set; }
    public string? Family { get; set; }
    public string? TaxonId { get; set; }
    public string? ImageUrl { get; set; }
    public string? SourceType { get; set; }
    public string? SourceId { get; set; }

    public PlantKnowledgeModel? Knowledge { get; set; }
}

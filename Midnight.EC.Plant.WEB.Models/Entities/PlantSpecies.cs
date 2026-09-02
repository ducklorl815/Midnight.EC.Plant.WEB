namespace Midnight.EC.Plant.WEB.Models.Entities;

public class PlantSpecies
{
    public int Id { get; set; }
    public string ScientificName { get; set; } = string.Empty;
    public string? CommonName { get; set; }
    public string? ChineseName { get; set; }
    public string? Genus { get; set; }
    public string? Family { get; set; }
    public string? TaxonId { get; set; }
    public string? ImageUrl { get; set; }
    public string? SourceType { get; set; }
    public string? SourceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public PlantKnowledge? Knowledge { get; set; }
    public ICollection<Plant> Plants { get; set; } = [];
    public ICollection<PlantSource> Sources { get; set; } = [];
    public ICollection<PlantSourceSpecies> SourceLinks { get; set; } = [];
    public ICollection<PlantKnowledgeSyncLog> SyncLogs { get; set; } = [];
}

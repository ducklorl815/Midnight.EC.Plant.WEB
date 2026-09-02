using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Entities;

public class PlantSource
{
    public int Id { get; set; }
    public int SpeciesId { get; set; }
    public SourceType SourceType { get; set; }
    public string? Title { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Author { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Language { get; set; }
    public string? ContentHash { get; set; }
    public int ReliabilityLevel { get; set; } = 3;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public PlantSpecies Species { get; set; } = null!;
    public ICollection<PlantSourceContent> Contents { get; set; } = [];
    public ICollection<PlantSourceSpecies> LinkedSpecies { get; set; } = [];
}

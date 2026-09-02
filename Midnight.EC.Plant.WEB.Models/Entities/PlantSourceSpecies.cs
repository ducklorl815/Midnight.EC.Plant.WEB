namespace Midnight.EC.Plant.WEB.Models.Entities;

public class PlantSourceSpecies
{
    public int SourceId { get; set; }
    public int SpeciesId { get; set; }

    public PlantSource Source { get; set; } = null!;
    public PlantSpecies Species { get; set; } = null!;
}

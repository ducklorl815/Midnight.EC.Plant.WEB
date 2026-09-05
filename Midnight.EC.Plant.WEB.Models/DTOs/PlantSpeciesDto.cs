namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantSpeciesDto
{
    public Guid Id { get; set; }
    public string ScientificName { get; set; } = string.Empty;
    public string? CommonName { get; set; }
    public string? ChineseName { get; set; }
    public string? Genus { get; set; }
    public string? Family { get; set; }
    public string? TaxonId { get; set; }
    public string? ImageUrl { get; set; }
}

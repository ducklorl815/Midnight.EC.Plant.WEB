namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantSourceDto
{
    public Guid Id { get; set; }
    public Guid SpeciesId { get; set; }
    public string? Title { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Summary { get; set; }
}

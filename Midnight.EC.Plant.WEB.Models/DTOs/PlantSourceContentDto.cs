namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantSourceContentDto
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public string? SourceTitle { get; set; }
    public string? SourceUrl { get; set; }
    public int ReliabilityLevel { get; set; }
    public string? CleanText { get; set; }
    public string? Summary { get; set; }
    public string? Keywords { get; set; }
}

public class PlantCitationDto
{
    public string SourceTitle { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public int ReliabilityLevel { get; set; }
    public string? Excerpt { get; set; }
}

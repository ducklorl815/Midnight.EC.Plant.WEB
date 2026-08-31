using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Entities;

public class PlantSourceContent
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public string? RawText { get; set; }
    public string? CleanText { get; set; }
    public string? Summary { get; set; }
    public string? Keywords { get; set; }
    public string? ParsedJson { get; set; }
    public string? ParserType { get; set; }
    public string? ParserVersion { get; set; }
    public string? ContentHash { get; set; }
    public SourceContentStatus Status { get; set; } = SourceContentStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? ParsedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public PlantSource Source { get; set; } = null!;
}

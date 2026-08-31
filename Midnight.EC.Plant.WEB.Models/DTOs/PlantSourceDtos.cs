using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class ParsedContentDto
{
    public string NormalizedUrl { get; set; } = string.Empty;
    public string UrlHash { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Domain { get; set; }
    public string? RawText { get; set; }
    public string? CleanText { get; set; }
    public string? Summary { get; set; }
    public string? Keywords { get; set; }
    public string? ContentHash { get; set; }
    public string ParserType { get; set; } = string.Empty;
    public string ParserVersion { get; set; } = "1.0";
    public SourceContentStatus Status { get; set; } = SourceContentStatus.Completed;
    public string? ErrorMessage { get; set; }
    public int ReliabilityLevel { get; set; } = 3;
    public bool IsExisting { get; set; }
    public int? ExistingSourceId { get; set; }
}

public class PlantSourceDetailDto
{
    public int Id { get; set; }
    public int SpeciesId { get; set; }
    public string? SpeciesName { get; set; }
    public SourceType SourceType { get; set; }
    public string? Title { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Author { get; set; }
    public int ReliabilityLevel { get; set; }
    public string? ContentHash { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public PlantSourceContentDetailDto? LatestContent { get; set; }
}

public class PlantSourceContentDetailDto
{
    public int Id { get; set; }
    public string? CleanText { get; set; }
    public string? Summary { get; set; }
    public string? Keywords { get; set; }
    public string? ContentHash { get; set; }
    public string? ParserType { get; set; }
    public SourceContentStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ParsedAt { get; set; }
}

public class PlantSourceListItemDto
{
    public int Id { get; set; }
    public int SpeciesId { get; set; }
    public string? SpeciesName { get; set; }
    public string? Title { get; set; }
    public string Url { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public int ReliabilityLevel { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

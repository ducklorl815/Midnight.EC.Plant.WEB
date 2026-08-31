using System.ComponentModel.DataAnnotations;
using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.ViewModels;

public class AdminPlantSourceListViewModel
{
    public List<AdminPlantSourceListItemViewModel> Sources { get; set; } = [];
}

public class AdminPlantSourceListItemViewModel
{
    public int Id { get; set; }
    public string? SpeciesName { get; set; }
    public string? Title { get; set; }
    public string Url { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public int ReliabilityLevel { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminPlantSourceCreateViewModel
{
    [Required(ErrorMessage = "請選擇物種")]
    [Display(Name = "關聯物種")]
    public int SpeciesId { get; set; }

    [Required(ErrorMessage = "請輸入 URL")]
    [Display(Name = "URL")]
    public string Url { get; set; } = string.Empty;

    [Display(Name = "標題（可選）")]
    public string? Title { get; set; }

    [Display(Name = "來源類型（可選，空白則自動判斷）")]
    public SourceType? SourceType { get; set; }

    public ParsedPreviewViewModel? Preview { get; set; }
    public List<SpeciesOptionViewModel> SpeciesOptions { get; set; } = [];
}

public class ParsedPreviewViewModel
{
    public string NormalizedUrl { get; set; } = string.Empty;
    public string UrlHash { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Domain { get; set; }
    public string? Summary { get; set; }
    public string? Keywords { get; set; }
    public string? CleanText { get; set; }
    public string? ContentHash { get; set; }
    public string ParserType { get; set; } = string.Empty;
    public string ParserVersion { get; set; } = "1.0";
    public SourceContentStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public int ReliabilityLevel { get; set; }
    public bool IsExisting { get; set; }
    public int? ExistingSourceId { get; set; }
    public string? RawText { get; set; }
}

public class AdminPlantSourceDetailViewModel
{
    public int Id { get; set; }
    public string? SpeciesName { get; set; }
    public SourceType SourceType { get; set; }
    public string? Title { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Author { get; set; }
    public int ReliabilityLevel { get; set; }
    public string? ContentHash { get; set; }
    public ParsedPreviewViewModel? LatestContent { get; set; }
}

public class SpeciesOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

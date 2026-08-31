namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantImageDto
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public int? DiaryId { get; set; }
    public string? Note { get; set; }
    public bool IsCover { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public PlantAnalysisDto? LatestAnalysis { get; set; }
}

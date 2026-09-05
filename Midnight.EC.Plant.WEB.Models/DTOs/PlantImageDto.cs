namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantImageDto
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public Guid? DiaryId { get; set; }
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
    public DateTime CreateDate { get; set; }
    public DateTime CreatedAt { get => CreateDate; set => CreateDate = value; }
    public PlantAnalysisDto? LatestAnalysis { get; set; }
}

namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantImageModel
{
    public Guid ID { get; set; }
    public Guid Id { get => ID; set => ID = value; }
    public int Seq { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime CreatedAt { get => CreateDate; set => CreateDate = value; }
    public DateTime ModifyDate { get; set; }
    public DateTime UpdatedAt { get => ModifyDate; set => ModifyDate = value; }
    public bool Enabled { get; set; } = true;
    public bool IsActive { get => Enabled; set => Enabled = value; }
    public bool Deleted { get; set; }
    public Guid PlantID { get; set; }
    public Guid PlantId { get => PlantID; set => PlantID = value; }
    public Guid? DiaryID { get; set; }
    public Guid? DiaryId { get => DiaryID; set => DiaryID = value; }
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
    public string? Sha256 { get; set; }
}

using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantEffectImageModel
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
    public Guid OriginalPhotoID { get; set; }
    public Guid OriginalPhotoId { get => OriginalPhotoID; set => OriginalPhotoID = value; }
    public string GeneratedImagePath { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string Layout { get; set; } = string.Empty;
    public string? ColorPalette { get; set; }
    public string? DecorationJson { get; set; }
    public string PromptVersion { get; set; } = string.Empty;
    public string? PromptText { get; set; }
    public EffectImageStatus Status { get; set; } = EffectImageStatus.Pending;
    public string? GenerationRequestId { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsLatest { get; set; } = true;
}

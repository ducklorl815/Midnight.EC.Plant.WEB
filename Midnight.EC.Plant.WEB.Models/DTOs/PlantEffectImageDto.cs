using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantEffectImageDto
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public Guid OriginalPhotoId { get; set; }
    public string GeneratedImagePath { get; set; } = string.Empty;
    public string? GeneratedImageUrl { get; set; }
    public string Style { get; set; } = string.Empty;
    public string Layout { get; set; } = string.Empty;
    public string? ColorPalette { get; set; }
    public string? DecorationJson { get; set; }
    public string PromptVersion { get; set; } = string.Empty;
    public EffectImageStatus Status { get; set; }
    public string? GenerationRequestId { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsLatest { get; set; }
    public DateTime CreateDate { get; set; }
    public PlantEffectOverlayDto? Overlay { get; set; }
    /// <summary>
    /// True when GeneratedImageUrl is the flattened final (base + deterministic card).
    /// False when it is AI base only (Phase 5 staged workflow).
    /// </summary>
    public bool IsComposedFinal { get; set; }
}

public class PlantEffectOverlayDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string? ScientificName { get; set; }
    public string Intro { get; set; } = string.Empty;
    public string SpecimenLabel { get; set; } = "SPECIMEN";
    public List<PlantDetailWallCareFactDto> CareFacts { get; set; } = [];
}

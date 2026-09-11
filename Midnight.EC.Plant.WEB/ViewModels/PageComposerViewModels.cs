using Midnight.EC.Plant.WEB.Models.DTOs;

namespace Midnight.EC.Plant.WEB.ViewModels;

public class PageComposerViewModel
{
    public PageComposerLayoutDto Layout { get; set; } = new();
    public bool IsEditorPreview { get; set; }
    public Dictionary<string, List<PlantDetailWallSlideDto>> CarouselSlidesByModuleId { get; set; } = new();
    public List<NotificationReminderItemDto> NotificationReminders { get; set; } = [];
}

public class PageComposerEditorViewModel
{
    public PageComposerLayoutDto Layout { get; set; } = new();
    public Dictionary<string, List<PlantDetailWallSlideDto>> CarouselSlidesByModuleId { get; set; } = new();
    public List<NotificationReminderItemDto> NotificationReminders { get; set; } = [];
    public List<PageComposerPhotoCatalogPlantDto> PhotoCatalog { get; set; } = [];
}

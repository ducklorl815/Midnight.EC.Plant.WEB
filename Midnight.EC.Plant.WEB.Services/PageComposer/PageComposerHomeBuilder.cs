using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Services.Plant;
using Midnight.EC.Plant.WEB.Services.PlantReminder;

namespace Midnight.EC.Plant.WEB.Services.PageComposer;

public class PageComposerHomeBuilder
{
    private readonly PageComposerService _composerService;
    private readonly PhotoWallLayoutService _photoWallLayoutService;
    private readonly PlantRespo _plantRespo;
    private readonly PlantImageRespo _imageRespo;
    private readonly IImageStorageService _imageStorage;
    private readonly PlantReminderService _reminderService;
    private readonly ILogger<PageComposerHomeBuilder> _logger;

    public PageComposerHomeBuilder(
        PageComposerService composerService,
        PhotoWallLayoutService photoWallLayoutService,
        PlantRespo plantRespo,
        PlantImageRespo imageRespo,
        IImageStorageService imageStorage,
        PlantReminderService reminderService,
        ILogger<PageComposerHomeBuilder> logger)
    {
        _composerService = composerService;
        _photoWallLayoutService = photoWallLayoutService;
        _plantRespo = plantRespo;
        _imageRespo = imageRespo;
        _imageStorage = imageStorage;
        _reminderService = reminderService;
        _logger = logger;
    }

    public async Task<(
            PageComposerLayoutDto Layout,
            Dictionary<string, List<PlantDetailWallSlideDto>> Slides,
            List<NotificationReminderItemDto> Notifications)>
        BuildAsync(CancellationToken cancellationToken = default)
    {
        var layout = await _composerService.GetHomeAsync(cancellationToken);
        await ResolveBannerImagesAsync(layout, cancellationToken);
        var slides = await BuildCarouselSlidesAsync(layout, cancellationToken);
        var notifications = await BuildNotificationsAsync(layout, cancellationToken);
        return (layout, slides, notifications);
    }

    public async Task<List<PageComposerPhotoCatalogPlantDto>> GetPhotoCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var plants = await _plantRespo.GetAllActiveAsync(cancellationToken);
        var result = new List<PageComposerPhotoCatalogPlantDto>();
        foreach (var plant in plants.OrderBy(p => p.NickName ?? p.Name, StringComparer.OrdinalIgnoreCase))
        {
            var photos = await _imageRespo.GetByPlantIdAsync(plant.ID, cancellationToken);
            if (photos.Count == 0) continue;
            result.Add(new PageComposerPhotoCatalogPlantDto
            {
                PlantId = plant.ID,
                DisplayName = string.IsNullOrWhiteSpace(plant.NickName) ? plant.Name : plant.NickName!,
                Photos = photos.Select(p => new PageComposerPhotoCatalogImageDto
                {
                    ImageId = p.ID,
                    Url = _imageStorage.GetPublicPath(p.StoragePath),
                    IsCover = p.IsCover
                }).ToList()
            });
        }

        return result;
    }

    public async Task ResolveBannerImagesAsync(
        PageComposerLayoutDto layout,
        CancellationToken cancellationToken = default)
    {
        foreach (var module in layout.Modules.Where(m => m.Type == PageModuleType.Banner))
        {
            module.Images ??= [];
            if (module.Images.Count == 0)
                module.Images.Add(new PageComposerImageDto { Alt = "Banner" });

            foreach (var img in module.Images)
                await ResolveOneBannerImageAsync(img, cancellationToken);
        }
    }

    private async Task ResolveOneBannerImageAsync(
        PageComposerImageDto img,
        CancellationToken cancellationToken)
    {
        var source = (img.Source ?? "url").Trim().ToLowerInvariant();
        if (source == "plant" || (img.PlantId is Guid && img.ImageId is Guid && source != "upload"))
        {
            img.Source = "plant";
            if (img.PlantId is not Guid plantId || img.ImageId is not Guid imageId ||
                plantId == Guid.Empty || imageId == Guid.Empty)
            {
                img.Url = string.Empty;
                img.Missing = true;
                return;
            }

            var row = await _imageRespo.GetByIdAsync(imageId, cancellationToken);
            if (row == null || row.PlantID != plantId || row.Deleted || !row.Enabled)
            {
                img.Url = string.Empty;
                img.Missing = true;
                return;
            }

            img.Url = _imageStorage.GetPublicPath(row.StoragePath);
            img.Missing = false;
            return;
        }

        if (source == "upload")
        {
            img.Source = "upload";
            img.PlantId = null;
            img.ImageId = null;
            img.Missing = string.IsNullOrWhiteSpace(img.Url);
            return;
        }

        // url / legacy
        if (string.IsNullOrWhiteSpace(img.Source))
            img.Source = string.IsNullOrWhiteSpace(img.Url) ? "url" : "url";
        img.Missing = false;
    }

    private async Task<List<NotificationReminderItemDto>> BuildNotificationsAsync(
        PageComposerLayoutDto layout,
        CancellationToken cancellationToken)
    {
        var enabled = layout.Modules.Any(m =>
            m.Enabled && m.Type == PageModuleType.NotificationReminders);
        if (!enabled)
            return [];

        try
        {
            return await _reminderService.GetFiredNotificationItemsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load notification reminder items.");
            return [];
        }
    }

    public async Task<Dictionary<string, List<PlantDetailWallSlideDto>>> BuildCarouselSlidesAsync(
        PageComposerLayoutDto layout,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, List<PlantDetailWallSlideDto>>(StringComparer.Ordinal);
        var carouselModules = layout.Modules
            .Where(m => m.Type == PageModuleType.PlantDetailWallCarousel)
            .ToList();
        if (carouselModules.Count == 0)
            return result;

        List<PlantDetailWallSlideDto> baseSlides;
        try
        {
            baseSlides = await LoadWallSlidesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load plant detail wall slides.");
            baseSlides = [];
        }

        if (baseSlides.Count == 0)
            baseSlides = PlaceholderSlides();

        foreach (var module in carouselModules)
            result[module.Id] = ApplyModuleFrames(baseSlides, module);

        return result;
    }

    private static List<PlantDetailWallSlideDto> ApplyModuleFrames(
        IReadOnlyList<PlantDetailWallSlideDto> baseSlides,
        PageComposerModuleDto module)
    {
        var frames = (module.SlideFrames ?? [])
            .Where(f => f.PlantId != Guid.Empty)
            .GroupBy(f => f.PlantId)
            .ToDictionary(g => g.Key, g => g.First());

        return baseSlides.Select(s =>
        {
            var zoom = 1d;
            var fx = 50d;
            var fy = 50d;
            if (frames.TryGetValue(s.PlantId, out var frame))
            {
                zoom = frame.Zoom <= 0 ? 1 : frame.Zoom;
                fx = frame.FocusX;
                fy = frame.FocusY;
            }

            return CloneWithFrame(s, zoom, fx, fy);
        }).ToList();
    }

    private static PlantDetailWallSlideDto CloneWithFrame(
        PlantDetailWallSlideDto source,
        double zoom,
        double focusX,
        double focusY) => new()
    {
        PlantId = source.PlantId,
        DisplayName = source.DisplayName,
        ScientificName = source.ScientificName,
        Intro = source.Intro,
        CoverImagePath = source.CoverImagePath,
        Zoom = zoom <= 0 ? 1 : Math.Clamp(zoom, 0.05, 8),
        FocusX = Math.Clamp(focusX, 0, 100),
        FocusY = Math.Clamp(focusY, 0, 100),
        CareFacts = source.CareFacts?.Select(f => new PlantDetailWallCareFactDto
        {
            Label = f.Label,
            Value = f.Value
        }).ToList() ?? []
    };

    private async Task<List<PlantDetailWallSlideDto>> LoadWallSlidesAsync(CancellationToken cancellationToken)
    {
        var plants = await _plantRespo.GetAllActiveAsync(cancellationToken);
        if (plants.Count == 0)
            return [];

        var plantIds = plants.Select(p => p.ID).ToList();
        var covers = await _imageRespo.GetCoversByPlantIdsAsync(plantIds, cancellationToken);

        var seeds = plants.Select(p =>
        {
            covers.TryGetValue(p.ID, out var cover);
            var path = cover == null ? null : _imageStorage.GetPublicPath(cover.StoragePath);
            var display = PlantDetailWallMapper.ResolveDisplayName(p);
            return new PlantWallSeed(p.ID, display, path, cover?.CreateDate);
        }).ToList();

        var wall = await _photoWallLayoutService.GetMergedLayoutAsync(seeds, cancellationToken);
        var orderedTiles = wall.Tiles.OrderBy(t => t.Order).ToList();
        var byId = plants.ToDictionary(p => p.ID);

        var slides = new List<PlantDetailWallSlideDto>();
        foreach (var tile in orderedTiles)
        {
            if (!byId.TryGetValue(tile.PlantId, out var plant))
                continue;
            covers.TryGetValue(plant.ID, out var cover);
            var path = cover == null ? null : _imageStorage.GetPublicPath(cover.StoragePath);
            slides.Add(PlantDetailWallMapper.ToSlide(plant, null, path));
        }

        return slides;
    }

    private static List<PlantDetailWallSlideDto> PlaceholderSlides() =>
    [
        new PlantDetailWallSlideDto
        {
            PlantId = Guid.Empty,
            DisplayName = "玉露錦",
            ScientificName = "Haworthia cooperi f. variegata",
            Intro = "是一種需要明亮散射光和良好通風的植物，特別適合在適當的溫度和濕度下生長。",
            CoverImagePath = null,
            CareFacts =
            [
                new() { Label = "建議日照", Value = "散射" },
                new() { Label = "光照", Value = "明亮散射光，可短時間柔和直射；錦斑品種避免強烈午後直射。" },
                new() { Label = "建議澆水", Value = "每 10 天" },
                new() { Label = "濕度", Value = "40–60%，通風良好即可。" },
                new() { Label = "溫度", Value = "10.00°C ~ 28.00°C" },
                new() { Label = "土壤", Value = "顆粒土為主、排水佳；可混少量泥炭。" },
                new() { Label = "施肥", Value = "平衡液肥；NPK 20-20-20；稀釋 1000–2000 倍；生長季每1–2個月1次；冬季停肥" },
                new() { Label = "生長季", Value = "春季至秋季為主要生長季節。" }
            ]
        },
        new PlantDetailWallSlideDto
        {
            PlantId = Guid.Empty,
            DisplayName = "範例植栽二",
            ScientificName = "Example species",
            Intro = "尚無真實植栽時顯示的佔位輪播。新增盆栽並編輯照片牆後，此區會自動帶入。",
            CoverImagePath = null,
            CareFacts =
            [
                new() { Label = "建議日照", Value = "未設定" },
                new() { Label = "光照", Value = "未設定" },
                new() { Label = "澆水", Value = "未設定" },
                new() { Label = "濕度", Value = "未設定" },
                new() { Label = "溫度", Value = "未設定" },
                new() { Label = "土壤", Value = "未設定" },
                new() { Label = "施肥", Value = "未設定" },
                new() { Label = "生長季", Value = "未設定" }
            ]
        }
    ];
}

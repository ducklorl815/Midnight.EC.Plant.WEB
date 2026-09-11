using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Models.Respository;

namespace Midnight.EC.Plant.WEB.Services.PageComposer;

public class PageComposerService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly PageComposerLayoutRespo _layoutRespo;
    private readonly ILogger<PageComposerService> _logger;

    public PageComposerService(
        PageComposerLayoutRespo layoutRespo,
        ILogger<PageComposerService> logger)
    {
        _layoutRespo = layoutRespo;
        _logger = logger;
    }

    public async Task<PageComposerLayoutDto> GetHomeAsync(CancellationToken cancellationToken = default)
    {
        var stored = await TryLoadAsync(cancellationToken);
        return stored ?? SeedDefault();
    }

    public async Task SaveAsync(PageComposerLayoutDto layout, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(layout);
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        await _layoutRespo.UpsertAsync(new PageComposerLayoutModel
        {
            LayoutKey = PageComposerLayoutDto.DefaultKey,
            LayoutJson = json
        }, cancellationToken);
    }

    private async Task<PageComposerLayoutDto?> TryLoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var row = await _layoutRespo.GetByKeyAsync(PageComposerLayoutDto.DefaultKey, cancellationToken);
            if (row == null || string.IsNullOrWhiteSpace(row.LayoutJson))
                return null;

            var parsed = JsonSerializer.Deserialize<PageComposerLayoutDto>(row.LayoutJson, JsonOptions);
            return parsed == null ? null : Normalize(parsed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load page composer layout; using seed.");
            return null;
        }
    }

    public static PageComposerLayoutDto SeedDefault()
    {
        return Normalize(new PageComposerLayoutDto
        {
            Version = PageComposerLayoutDto.CurrentVersion,
            Modules =
            [
                CreateBannerPlaceholder(0),
                CreateNotificationReminders(1),
                CreatePlaceholder(PageModuleType.LeftImageRightText, 2, "theme-moss",
                    "晨光窗台", "左圖右文佔位",
                    "這是左圖右文模組的佔位內容。之後可換成真實盆栽故事與照片。"),
                CreatePlaceholder(PageModuleType.RightImageLeftText, 3, "theme-clay",
                    "午後葉影", "右圖左文佔位",
                    "這是右圖左文模組的佔位內容。風格可獨立調整，拼起來仍是同一站。"),
                CreateMultiTextPlaceholder(4, "theme-fern"),
                CreateMultiImagePlaceholder(5, "theme-stone"),
                CreatePlantDetailWallCarousel(6)
            ]
        });
    }

    public static PageComposerModuleDto CreateBannerPlaceholder(int order)
    {
        return new PageComposerModuleDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = PageModuleType.Banner,
            Enabled = true,
            Order = order,
            Title = "探索每一盆綠意",
            Subtitle = "由上而下，一區一景——模組拼圖組成的植栽故事首頁。",
            Theme = "theme-banner",
            Images = [new PageComposerImageDto { Url = "", Alt = "Banner" }],
            CtaText = "新增植栽",
            CtaHref = "/Plant/Create"
        };
    }

    public static PageComposerModuleDto CreatePlaceholder(
        PageModuleType type,
        int order,
        string theme,
        string title,
        string subtitle,
        string body)
    {
        return new PageComposerModuleDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = type,
            Enabled = true,
            Order = order,
            Title = title,
            Subtitle = subtitle,
            Body = body,
            Theme = theme,
            Images =
            [
                new PageComposerImageDto { Url = "", Alt = title }
            ]
        };
    }

    public static PageComposerModuleDto CreateModuleOfType(PageModuleType type)
    {
        return type switch
        {
            PageModuleType.Banner => CreateBannerPlaceholder(0),
            PageModuleType.LeftImageRightText => CreatePlaceholder(
                type, 0, "theme-moss", "新左圖右文", "佔位副標", "請之後替換成真實內容。"),
            PageModuleType.RightImageLeftText => CreatePlaceholder(
                type, 0, "theme-clay", "新右圖左文", "佔位副標", "請之後替換成真實內容。"),
            PageModuleType.MultiImageMultiText => CreateMultiTextPlaceholder(0, "theme-fern"),
            PageModuleType.MultiImageStyle => CreateMultiImagePlaceholder(0, "theme-stone"),
            PageModuleType.PlantDetailWallCarousel => CreatePlantDetailWallCarousel(0),
            PageModuleType.NotificationReminders => CreateNotificationReminders(0),
            _ => CreatePlaceholder(PageModuleType.LeftImageRightText, 0, "theme-default", "新模組", "佔位副標", "佔位")
        };
    }

    public static PageComposerModuleDto CreateNotificationReminders(int order)
    {
        return new PageComposerModuleDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = PageModuleType.NotificationReminders,
            Enabled = true,
            Order = order,
            Title = "通知提醒",
            Subtitle = null,
            Body = null,
            Theme = "theme-default",
            Images = [],
            Items = [],
            SlideFrames = []
        };
    }

    private static PageComposerModuleDto CreatePlantDetailWallCarousel(int order)
    {
        return new PageComposerModuleDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = PageModuleType.PlantDetailWallCarousel,
            Enabled = true,
            Order = order,
            Title = "植栽細節牆 - 輪播",
            Subtitle = null,
            Body = null,
            Theme = "theme-detail-wall",
            Images = [],
            Items = [],
            SlideFrames = []
        };
    }

    private static PageComposerModuleDto CreateMultiTextPlaceholder(int order, string theme)
    {
        return new PageComposerModuleDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = PageModuleType.MultiImageMultiText,
            Enabled = true,
            Order = order,
            Title = "多圖多文牆",
            Subtitle = "一區多則故事",
            Body = "多圖多文模組佔位：之後可放多盆植物或文章摘要。",
            Theme = theme,
            Items =
            [
                new PageComposerItemDto { Title = "故事一", Body = "佔位說明文字 A。", ImageUrl = "" },
                new PageComposerItemDto { Title = "故事二", Body = "佔位說明文字 B。", ImageUrl = "" },
                new PageComposerItemDto { Title = "故事三", Body = "佔位說明文字 C。", ImageUrl = "" }
            ]
        };
    }

    private static PageComposerModuleDto CreateMultiImagePlaceholder(int order, string theme)
    {
        return new PageComposerModuleDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = PageModuleType.MultiImageStyle,
            Enabled = true,
            Order = order,
            Title = "多圖風格",
            Subtitle = "以影像為主",
            Body = null,
            Theme = theme,
            Images =
            [
                new PageComposerImageDto { Url = "", Alt = "圖一" },
                new PageComposerImageDto { Url = "", Alt = "圖二" },
                new PageComposerImageDto { Url = "", Alt = "圖三" },
                new PageComposerImageDto { Url = "", Alt = "圖四" }
            ]
        };
    }

    public static PageComposerLayoutDto Normalize(PageComposerLayoutDto layout)
    {
        layout.Modules ??= [];
        MigrateLegacyBanner(layout);
        EnsureBannerPinned(layout);
        EnsureNotificationRemindersModule(layout);

        var i = 0;
        foreach (var m in layout.Modules.OrderBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(m.Id))
                m.Id = Guid.NewGuid().ToString("N");
            m.Order = i++;
            if (m.Type == PageModuleType.Banner)
                m.Enabled = true;
            m.Theme = string.IsNullOrWhiteSpace(m.Theme)
                ? (m.Type == PageModuleType.Banner ? "theme-banner" : "theme-default")
                : m.Theme.Trim();
            m.Images ??= [];
            if (m.Type == PageModuleType.Banner)
                NormalizeBannerImages(m);
            m.Items ??= [];
            m.SlideFrames ??= [];
            m.Title ??= string.Empty;
            if (m.Type == PageModuleType.NotificationReminders && string.IsNullOrWhiteSpace(m.Title))
                m.Title = "通知提醒";
            NormalizeSlideFrames(m);
        }

        layout.Modules = layout.Modules.OrderBy(x => x.Order).ToList();
        layout.Version = PageComposerLayoutDto.CurrentVersion;
        layout.LegacyBanner = null;
        layout.LegacyShellOrder = null;
        return layout;
    }

    private static void NormalizeBannerImages(PageComposerModuleDto module)
    {
        if (module.Images.Count == 0)
            module.Images.Add(new PageComposerImageDto { Alt = "Banner", Source = "url" });

        var img = module.Images[0];
        module.Images = [img];
        img.Alt ??= "Banner";
        img.Missing = false;

        var source = (img.Source ?? "").Trim().ToLowerInvariant();
        if (source == "plant" || (img.PlantId is Guid pid && img.ImageId is Guid iid && pid != Guid.Empty && iid != Guid.Empty && source != "upload" && source != "url"))
        {
            img.Source = "plant";
            // 持久化以 id 為準；不依賴快取 url
            img.Url = string.Empty;
            return;
        }

        if (source == "upload")
        {
            img.Source = "upload";
            img.PlantId = null;
            img.ImageId = null;
            img.Url = img.Url?.Trim() ?? string.Empty;
            return;
        }

        img.Source = "url";
        img.PlantId = null;
        img.ImageId = null;
        img.Url = img.Url?.Trim() ?? string.Empty;
    }

    /// <summary>Banner 必須存在、唯一、釘在 order 0、且永遠啟用。</summary>
    private static void EnsureBannerPinned(PageComposerLayoutDto layout)
    {
        var banners = layout.Modules.Where(m => m.Type == PageModuleType.Banner).ToList();
        if (banners.Count == 0)
        {
            layout.Modules.Insert(0, CreateBannerPlaceholder(0));
            return;
        }

        var keep = banners[0];
        keep.Enabled = true;
        foreach (var extra in banners.Skip(1))
            layout.Modules.Remove(extra);

        layout.Modules.Remove(keep);
        layout.Modules.Insert(0, keep);
        for (var i = 0; i < layout.Modules.Count; i++)
            layout.Modules[i].Order = i;
    }

    private static void EnsureNotificationRemindersModule(PageComposerLayoutDto layout)
    {
        if (layout.Modules.Any(m => m.Type == PageModuleType.NotificationReminders))
            return;

        var module = CreateNotificationReminders(0);
        var bannerIndex = layout.Modules.FindIndex(m => m.Type == PageModuleType.Banner);
        if (bannerIndex < 0)
        {
            layout.Modules.Insert(0, module);
        }
        else
        {
            layout.Modules.Insert(bannerIndex + 1, module);
        }

        for (var i = 0; i < layout.Modules.Count; i++)
            layout.Modules[i].Order = i;
    }

    private static void NormalizeSlideFrames(PageComposerModuleDto module)
    {
        if (module.Type != PageModuleType.PlantDetailWallCarousel)
        {
            module.SlideFrames = [];
            return;
        }

        var cleaned = new List<PageComposerSlideFrameDto>();
        var seen = new HashSet<Guid>();
        foreach (var frame in module.SlideFrames)
        {
            if (frame.PlantId == Guid.Empty || !seen.Add(frame.PlantId))
                continue;
            cleaned.Add(new PageComposerSlideFrameDto
            {
                PlantId = frame.PlantId,
                Zoom = frame.Zoom <= 0 ? 1 : Math.Clamp(frame.Zoom, 0.05, 8),
                FocusX = Math.Clamp(frame.FocusX, 0, 100),
                FocusY = Math.Clamp(frame.FocusY, 0, 100)
            });
        }

        module.SlideFrames = cleaned;
    }

    private static void MigrateLegacyBanner(PageComposerLayoutDto layout)
    {
        if (layout.LegacyBanner == null) return;
        if (layout.Modules.Any(m => m.Type == PageModuleType.Banner))
        {
            layout.LegacyBanner = null;
            return;
        }

        var legacy = layout.LegacyBanner;
        var banner = new PageComposerModuleDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = PageModuleType.Banner,
            Enabled = legacy.Enabled,
            Title = string.IsNullOrWhiteSpace(legacy.Title) ? "Banner" : legacy.Title,
            Subtitle = legacy.Subtitle,
            Theme = "theme-banner",
            CtaText = legacy.CtaText,
            CtaHref = legacy.CtaHref,
            Images =
            [
                new PageComposerImageDto
                {
                    Url = legacy.ImageUrl ?? "",
                    Alt = legacy.Title
                }
            ]
        };

        var modulesFirst = string.Equals(layout.LegacyShellOrder, "ModulesFirst", StringComparison.OrdinalIgnoreCase);
        if (modulesFirst)
        {
            banner.Order = layout.Modules.Count == 0 ? 0 : layout.Modules.Max(m => m.Order) + 1;
            layout.Modules.Add(banner);
        }
        else
        {
            foreach (var m in layout.Modules)
                m.Order += 1;
            banner.Order = 0;
            layout.Modules.Insert(0, banner);
        }
    }
}

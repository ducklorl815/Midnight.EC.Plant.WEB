using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Services.PageComposer;
using Midnight.EC.Plant.WEB.Services.Plant;
using Midnight.EC.Plant.WEB.Services.PlantReminder;
using Midnight.EC.Plant.WEB.ViewModels;

namespace Midnight.EC.Plant.WEB.Controllers;

public class SettingsController : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly PhotoWallLayoutService _photoWallLayoutService;
    private readonly PageComposerService _pageComposerService;
    private readonly PageComposerHomeBuilder _pageComposerHomeBuilder;
    private readonly PlantReminderService _reminderService;
    private readonly CareGuideLayoutService _careGuideLayoutService;
    private readonly IImageStorageService _imageStorageService;

    public SettingsController(
        PhotoWallLayoutService photoWallLayoutService,
        PageComposerService pageComposerService,
        PageComposerHomeBuilder pageComposerHomeBuilder,
        PlantReminderService reminderService,
        CareGuideLayoutService careGuideLayoutService,
        IImageStorageService imageStorageService)
    {
        _photoWallLayoutService = photoWallLayoutService;
        _pageComposerService = pageComposerService;
        _pageComposerHomeBuilder = pageComposerHomeBuilder;
        _reminderService = reminderService;
        _careGuideLayoutService = careGuideLayoutService;
        _imageStorageService = imageStorageService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> PhotoWall(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "照片牆版面";
        ViewData["WallEditor"] = true;

        var model = await BuildEditorAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PhotoWall(string layoutJson, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "照片牆版面";
        ViewData["WallEditor"] = true;

        if (string.IsNullOrWhiteSpace(layoutJson))
        {
            TempData["Error"] = "沒有收到版面資料。";
            return RedirectToAction(nameof(PhotoWall));
        }

        PhotoWallLayoutDto? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<PhotoWallLayoutDto>(layoutJson, JsonOptions);
        }
        catch (JsonException)
        {
            TempData["Error"] = "版面資料格式不正確。";
            return RedirectToAction(nameof(PhotoWall));
        }

        if (parsed == null)
        {
            TempData["Error"] = "版面資料無法解析。";
            return RedirectToAction(nameof(PhotoWall));
        }

        var dashboard = await _reminderService.GetDashboardAsync(cancellationToken);
        var validIds = dashboard.Select(p => p.Id).ToHashSet();
        await _photoWallLayoutService.SaveAsync(parsed, validIds, cancellationToken);

        TempData["Success"] = "牆版面已儲存。";
        return RedirectToAction(nameof(PhotoWall));
    }

    [HttpGet]
    public async Task<IActionResult> CareGuideLayout(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "照護指南版面";
        var order = await _careGuideLayoutService.GetAsync(cancellationToken);
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CareGuideLayout(string layoutJson, string? sectionsJson, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "照護指南版面";
        var raw = !string.IsNullOrWhiteSpace(layoutJson) ? layoutJson : sectionsJson;
        if (string.IsNullOrWhiteSpace(raw))
        {
            TempData["Error"] = "沒有收到版面資料。";
            return RedirectToAction(nameof(CareGuideLayout));
        }

        try
        {
            CareGuideSectionOrderDto? parsed = null;
            // 新：完整 layout JSON
            try
            {
                parsed = JsonSerializer.Deserialize<CareGuideSectionOrderDto>(raw, JsonOptions);
            }
            catch (JsonException)
            {
                // 舊：僅字串陣列
                var sections = JsonSerializer.Deserialize<List<string>>(raw, JsonOptions);
                if (sections != null)
                {
                    parsed = new CareGuideSectionOrderDto { Sections = sections };
                }
            }

            if (parsed == null)
            {
                TempData["Error"] = "版面資料無法解析。";
                return RedirectToAction(nameof(CareGuideLayout));
            }

            await _careGuideLayoutService.SaveAsync(parsed, cancellationToken);
            TempData["Success"] = "照護指南區塊順序已儲存。";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"儲存失敗：{ex.Message}（若尚未建立 PageComposerLayout 資料表，請先執行 docs/sql/PageComposerLayout.sql）";
        }

        return RedirectToAction(nameof(CareGuideLayout));
    }

    [HttpGet]
    public async Task<IActionResult> PageComposer(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "首頁拼圖";
        ViewData["WallEditor"] = true;

        var (layout, slides, notifications) = await _pageComposerHomeBuilder.BuildAsync(cancellationToken);
        var catalog = await _pageComposerHomeBuilder.GetPhotoCatalogAsync(cancellationToken);
        return View(new PageComposerEditorViewModel
        {
            Layout = layout,
            CarouselSlidesByModuleId = slides,
            NotificationReminders = notifications,
            PhotoCatalog = catalog
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPageComposerBanner(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "請選擇圖片。" });

        if (file.Length > 12 * 1024 * 1024)
            return BadRequest(new { error = "圖片請小於 12MB。" });

        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp" or "image/gif"))
            return BadRequest(new { error = "僅支援 JPG／PNG／WebP／GIF。" });

        await using var stream = file.OpenReadStream();
        var stored = await _imageStorageService.SaveSiteMediaAsync(
            stream,
            file.FileName,
            file.ContentType ?? "image/jpeg",
            "page-composer/banner",
            cancellationToken);

        return Json(new
        {
            url = _imageStorageService.GetPublicPath(stored.StoragePath),
            source = "upload"
        });
    }

    [HttpGet]
    public async Task<IActionResult> PageComposerPhotoCatalog(CancellationToken cancellationToken)
    {
        var catalog = await _pageComposerHomeBuilder.GetPhotoCatalogAsync(cancellationToken);
        return Json(catalog);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PageComposer(string layoutJson, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "首頁拼圖";
        ViewData["WallEditor"] = true;

        if (string.IsNullOrWhiteSpace(layoutJson))
        {
            TempData["Error"] = "沒有收到拼圖資料。";
            return RedirectToAction(nameof(PageComposer));
        }

        PageComposerLayoutDto? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<PageComposerLayoutDto>(layoutJson, JsonOptions);
        }
        catch (JsonException)
        {
            TempData["Error"] = "拼圖資料格式不正確。";
            return RedirectToAction(nameof(PageComposer));
        }

        if (parsed == null)
        {
            TempData["Error"] = "拼圖資料無法解析。";
            return RedirectToAction(nameof(PageComposer));
        }

        try
        {
            await _pageComposerService.SaveAsync(parsed, cancellationToken);
        }
        catch (Exception)
        {
            TempData["Error"] = "儲存失敗。請先執行 docs/sql/PageComposerLayout.sql 建立資料表。";
            return RedirectToAction(nameof(PageComposer));
        }

        TempData["Success"] = "首頁拼圖已儲存。";
        return RedirectToAction(nameof(PageComposer));
    }

    private async Task<PhotoWallEditorViewModel> BuildEditorAsync(CancellationToken cancellationToken)
    {
        var dashboard = await _reminderService.GetDashboardAsync(cancellationToken);
        var cards = dashboard.Select(p => new PlantDashboardCardViewModel
        {
            Id = p.Id,
            Name = p.Name,
            NickName = p.NickName,
            DisplayName = p.DisplayName,
            CoverImagePath = p.CoverImagePath,
            LatestVisualActivityAt = p.LatestVisualActivityAt,
            IsWateringOverdue = p.IsWateringOverdue,
            DaysSinceLastWatering = p.DaysSinceLastWatering,
            WateringIntervalDays = p.WateringIntervalDays
        }).ToList();

        var seeds = cards
            .Select(c => new PlantWallSeed(c.Id, c.DisplayName, c.CoverImagePath, c.LatestVisualActivityAt))
            .ToList();
        var layout = await _photoWallLayoutService.GetMergedLayoutAsync(seeds, cancellationToken);
        var byId = cards.ToDictionary(c => c.Id);
        var packed = PhotoWallPacker.Pack(layout.Tiles, layout.Columns);
        var tiles = packed
            .Where(t => byId.ContainsKey(t.PlantId))
            .Select(t => new PhotoWallTileViewModel
            {
                Plant = byId[t.PlantId],
                ColSpan = t.ColSpan,
                RowSpan = t.RowSpan,
                Order = t.Order,
                Col = t.Col,
                Row = t.Row,
                Zoom = t.Zoom,
                FocusX = t.FocusX,
                FocusY = t.FocusY
            })
            .ToList();
        var rowCount = packed.Count == 0 ? 1 : packed.Max(t => t.Row + t.RowSpan);

        return new PhotoWallEditorViewModel
        {
            Layout = layout,
            Tiles = tiles,
            ColumnTemplate = PhotoWallPacker.BuildColumnTemplate(layout),
            RowTemplate = PhotoWallPacker.BuildRowTemplate(layout, rowCount),
            ContentRowCount = rowCount
        };
    }
}

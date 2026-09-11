using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Midnight.EC.Plant.WEB.Models.AI;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Services.External;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Services.PageComposer;
using Midnight.EC.Plant.WEB.Services.Plant;
using Midnight.EC.Plant.WEB.Services.PlantAnalysis;
using Midnight.EC.Plant.WEB.Services.PlantCare;
using Midnight.EC.Plant.WEB.Services.PlantDiary;
using Midnight.EC.Plant.WEB.Services.PlantKnowledge;
using Midnight.EC.Plant.WEB.Services.PlantProfile;
using Midnight.EC.Plant.WEB.Services.PlantReminder;
using Midnight.EC.Plant.WEB.Services.PlantTimeline;
using Midnight.EC.Plant.WEB.Utility.Json;
using Midnight.EC.Plant.WEB.ViewModels;

namespace Midnight.EC.Plant.WEB.Controllers;

public class PlantController : Controller
{
    private readonly PlantService _plantService;
    private readonly PlantDiaryService _diaryService;
    private readonly PlantImageService _imageService;
    private readonly PlantAnalysisService _analysisService;
    private readonly PlantCareService _careService;
    private readonly PlantProfileService _profileService;
    private readonly PlantReminderService _reminderService;
    private readonly PlantTimelineService _timelineService;
    private readonly PlantKnowledgeService _knowledgeService;
    private readonly PageComposerHomeBuilder _pageComposerHomeBuilder;
    private readonly CareGuideLayoutService _careGuideLayoutService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<PlantController> _logger;

    public PlantController(
        PlantService plantService,
        PlantDiaryService diaryService,
        PlantImageService imageService,
        PlantAnalysisService analysisService,
        PlantCareService careService,
        PlantProfileService profileService,
        PlantReminderService reminderService,
        PlantTimelineService timelineService,
        PlantKnowledgeService knowledgeService,
        PageComposerHomeBuilder pageComposerHomeBuilder,
        CareGuideLayoutService careGuideLayoutService,
        IImageStorageService imageStorageService,
        ILogger<PlantController> logger)
    {
        _plantService = plantService;
        _diaryService = diaryService;
        _imageService = imageService;
        _analysisService = analysisService;
        _careService = careService;
        _profileService = profileService;
        _reminderService = reminderService;
        _timelineService = timelineService;
        _knowledgeService = knowledgeService;
        _pageComposerHomeBuilder = pageComposerHomeBuilder;
        _careGuideLayoutService = careGuideLayoutService;
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["HomeChrome"] = true;
        var (layout, slides, notifications) = await _pageComposerHomeBuilder.BuildAsync(cancellationToken);
        return View(new PageComposerViewModel
        {
            Layout = layout,
            IsEditorPreview = false,
            CarouselSlidesByModuleId = slides,
            NotificationReminders = notifications
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteCareFromReminder(
        Guid plantId,
        Guid reminderId,
        CancellationToken cancellationToken)
    {
        try
        {
            var reminder = await _reminderService.GetActiveByPlantIdAsync(plantId, cancellationToken);
            var target = reminder.FirstOrDefault(r => r.Id == reminderId && r.PlantId == plantId);
            if (target == null)
            {
                TempData["Error"] = "找不到這則提醒。";
                return RedirectToAction(nameof(Index));
            }

            if (target.ReminderType is not (ReminderType.Watering or ReminderType.Fertilizing))
            {
                TempData["Error"] = "此提醒無法一鍵完成。";
                return RedirectToAction(nameof(Index));
            }

            var careType = target.ReminderType == ReminderType.Watering
                ? CareRecordType.Watering
                : CareRecordType.Fertilizing;
            var note = careType == CareRecordType.Watering
                ? "通知提醒：已澆水"
                : "通知提醒：已施肥";

            await _careService.CreateAsync(plantId, DateTime.Today, careType, null, null, note, cancellationToken);
            await _reminderService.SyncRemindersAsync(plantId, cancellationToken);
            TempData["Success"] = careType == CareRecordType.Watering ? "已標記今日澆水。" : "已標記今日施肥。";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CompleteCareFromReminder failed for plant {PlantId} reminder {ReminderId}", plantId, reminderId);
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchWater(Guid[]? plantIds, CancellationToken cancellationToken)
    {
        if (plantIds == null || plantIds.Length == 0)
        {
            TempData["Error"] = "請至少勾選一盆再批次已澆水。";
            return RedirectToAction(nameof(Index));
        }

        var today = DateTime.Today;
        var ok = 0;
        foreach (var id in plantIds.Distinct())
        {
            try
            {
                await _careService.CreateAsync(id, today, CareRecordType.Watering, null, null, "首頁批次已澆水", cancellationToken);
                await _reminderService.SyncRemindersAsync(id, cancellationToken);
                ok++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Batch water failed for plant {PlantId}", id);
            }
        }

        TempData["Success"] = $"已為 {ok} 盆標記今日澆水。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLastWateringDate(Guid plantId, DateTime lastWateringDate, CancellationToken cancellationToken)
    {
        if (lastWateringDate.Date > DateTime.Today)
        {
            TempData["Error"] = "上次澆水日不能是未來。";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _careService.CreateAsync(
                plantId,
                lastWateringDate.Date,
                CareRecordType.Watering,
                null,
                null,
                "補上次澆水日",
                cancellationToken);
            await _reminderService.SyncRemindersAsync(plantId, cancellationToken);
            TempData["Success"] = "已補上澆水日起點。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetLastWateringDate failed");
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private static PlantDashboardCardViewModel MapDashboardCard(PlantDashboardItemDto p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        NickName = p.NickName,
        DisplayName = p.DisplayName,
        SpeciesName = p.SpeciesName,
        SpeciesChineseName = p.SpeciesChineseName,
        SpeciesScientificName = p.SpeciesScientificName,
        Location = p.Location,
        CoverImagePath = p.CoverImagePath,
        LatestVisualActivityAt = p.LatestVisualActivityAt,
        LatestHealthScore = p.LatestHealthScore,
        ActiveReminderCount = p.ActiveReminderCount,
        OverdueReminderCount = p.OverdueReminderCount,
        DaysSinceLastWatering = p.DaysSinceLastWatering,
        WateringIntervalDays = p.WateringIntervalDays,
        IsWateringOverdue = p.IsWateringOverdue,
        MissingLastWateringDate = p.MissingLastWateringDate,
        KnowledgeIncomplete = p.KnowledgeIncomplete,
        EnvironmentIncomplete = p.EnvironmentIncomplete,
        ActualLightLabel = p.ActualLightLabel,
        TopReminders = p.TopReminders.Select(MapReminder).ToList()
    };

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreatePlantViewModel { WateredToday = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePlantViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.NickName) && !string.IsNullOrWhiteSpace(model.ChineseName))
        {
            model.NickName = model.ChineseName.Trim();
        }

        if (!IsCreateEnvironmentComplete(model))
        {
            ModelState.AddModelError(string.Empty, "請填齊實際環境六項後再查物種。");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _plantService.IsNickNameTakenAsync(model.NickName, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.NickName), $"暱稱「{model.NickName}」已被使用，請換一個。");
            return View(model);
        }

        IReadOnlyList<Midnight.EC.Plant.WEB.Models.External.ExternalSpeciesResult> candidates;
        try
        {
            candidates = await _plantService.SearchSpeciesCandidatesAsync(model.ChineseName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Species candidate search failed; allow pending create");
            candidates = [];
        }

        var confirm = new ConfirmSpeciesViewModel
        {
            Draft = model,
            Candidates = candidates.Select(MapCandidate).ToList(),
            SelectedIndex = candidates.Count > 0 ? 0 : null
        };

        return View("ConfirmSpecies", confirm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmSpecies(ConfirmSpeciesViewModel model, CancellationToken cancellationToken)
    {
        if (model.Draft == null || string.IsNullOrWhiteSpace(model.Draft.NickName))
        {
            ModelState.AddModelError(string.Empty, "建檔資料遺失，請重新開始。");
            return View("Create", model.Draft ?? new CreatePlantViewModel());
        }

        if (!IsCreateEnvironmentComplete(model.Draft))
        {
            ModelState.AddModelError(string.Empty, "實際環境不完整，請返回上一步補齊。");
            return View("Create", model.Draft);
        }

        if (model.SelectedIndex is null ||
            model.Candidates == null ||
            model.SelectedIndex < 0 ||
            model.SelectedIndex >= model.Candidates.Count)
        {
            ModelState.AddModelError(string.Empty, "請選擇一筆物種。");
            return View(model);
        }

        var selected = model.Candidates[model.SelectedIndex.Value];
        var external = ToExternal(selected);

        try
        {
            // 先同步物種知識，才能比對環境落差；尚未建檔
            var speciesId = await _plantService.EnsureSpeciesKnowledgeAsync(external, model.Draft.ChineseName, cancellationToken);

            var warnings = await BuildDraftMismatchWarningsAsync(speciesId, model.Draft, cancellationToken);
            if (warnings.Count > 0 && !model.AcknowledgeMismatch)
            {
                model.MismatchWarnings = warnings;
                ModelState.AddModelError(nameof(model.AcknowledgeMismatch), "實際環境與建議有落差，請勾選「我知道環境不理想」後再建立。");
                return View(model);
            }

            var draft = ToDraft(model.Draft);
            draft.EnvironmentMismatchAcknowledged = warnings.Count > 0;

            var plant = await _plantService.CreateFromDraftAsync(draft, external, cancellationToken);

            var envFit = await _knowledgeService.RefreshEnvironmentAdviceAsync(plant.Id, cancellationToken);
            TempData["Flash"] = envFit.Succeeded
                ? "植物已建立，並已同步物種知識與環境適配建議。"
                : envFit.SkippedNoEnvironment
                    ? "植物已建立，並已同步物種知識。"
                    : string.IsNullOrWhiteSpace(envFit.FailureReason)
                        ? "植物已建立，並已同步物種知識。"
                        : $"植物已建立並同步物種知識；環境適配建議失敗：{envFit.FailureReason}";

            return RedirectToAction(nameof(Details), new { id = plant.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmSpecies failed");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BackToCreate(ConfirmSpeciesViewModel model)
    {
        return View("Create", model.Draft ?? new CreatePlantViewModel { WateredToday = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchByScientificName(ConfirmSpeciesViewModel model, CancellationToken cancellationToken)
    {
        if (model.Draft == null)
        {
            return RedirectToAction(nameof(Create));
        }

        var query = !string.IsNullOrWhiteSpace(model.ManualChineseName)
            ? model.ManualChineseName.Trim()
            : model.ManualScientificName?.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            ModelState.AddModelError(nameof(model.ManualChineseName), "請輸入中文名或學名。");
            model.Candidates ??= [];
            return View("ConfirmSpecies", model);
        }

        if (!string.IsNullOrWhiteSpace(model.ManualChineseName))
        {
            model.Draft.ChineseName = model.ManualChineseName.Trim();
        }

        IReadOnlyList<Midnight.EC.Plant.WEB.Models.External.ExternalSpeciesResult> candidates;
        try
        {
            candidates = await _plantService.SearchSpeciesCandidatesAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Manual species search failed");
            ModelState.AddModelError(string.Empty, ex.Message);
            candidates = [];
        }

        model.Candidates = candidates.Select(MapCandidate).ToList();
        model.SelectedIndex = model.Candidates.Count > 0 ? 0 : null;
        if (model.Candidates.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "查無候選，可改寫中文名／學名後再試。");
        }

        return View("ConfirmSpecies", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWithoutSpecies(ConfirmSpeciesViewModel model, CancellationToken cancellationToken)
    {
        if (model.Draft == null || string.IsNullOrWhiteSpace(model.Draft.ChineseName))
        {
            return RedirectToAction(nameof(Create));
        }

        if (!IsCreateEnvironmentComplete(model.Draft))
        {
            ModelState.AddModelError(string.Empty, "請先填齊實際環境六項。");
            return View("Create", model.Draft);
        }

        try
        {
            var plant = await _plantService.CreateFromDraftAsync(ToDraft(model.Draft), null, cancellationToken);
            TempData["Flash"] = "已先建檔（物種未確認）。實際環境已保存；請之後在詳情頁同步知識（此時不打 AI）。";
            return RedirectToAction(nameof(Details), new { id = plant.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateWithoutSpecies failed");
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Candidates ??= [];
            return View("ConfirmSpecies", model);
        }
    }

    private static CreatePlantDraft ToDraft(CreatePlantViewModel model) => new()
    {
        ChineseName = model.ChineseName.Trim(),
        NickName = model.NickName.Trim(),
        Description = model.Description,
        StartDate = model.StartDate,
        WateredToday = model.WateredToday,
        ActualPlacement = model.ActualPlacement,
        ActualLight = model.ActualLight,
        HasRainCover = model.HasRainCover,
        SubstrateType = model.SubstrateType,
        SaucerState = model.SaucerState,
        City = model.City
    };

    private static bool IsCreateEnvironmentComplete(CreatePlantViewModel model) =>
        model.ActualPlacement != null
        && model.ActualLight != null
        && model.HasRainCover != null
        && !string.IsNullOrWhiteSpace(model.SubstrateType)
        && model.SaucerState != null
        && !string.IsNullOrWhiteSpace(model.City);

    private async Task<List<string>> BuildDraftMismatchWarningsAsync(
        Guid speciesId,
        CreatePlantViewModel draft,
        CancellationToken cancellationToken)
    {
        var knowledge = await _knowledgeService.GetBySpeciesIdAsync(speciesId, cancellationToken);
        if (knowledge == null)
        {
            return [];
        }

        var suggestedLight = knowledge.SuggestedLight
            ?? LightLevelDisplay.TryParseFromText(knowledge.LightRequirement);
        var taboos = CareConstraintExtractor.ExtractTaboos(
            knowledge.LightRequirement,
            knowledge.WaterRequirement,
            knowledge.SoilRequirement,
            knowledge.CareSummary,
            knowledge.ExternalCareGuide);

        return CareConstraintExtractor.BuildMismatchWarnings(
            suggestedLight,
            draft.ActualLight,
            taboos,
            draft.HasRainCover,
            draft.ActualPlacement,
            draft.SubstrateType);
    }

    private static SpeciesCandidateItemViewModel MapCandidate(Midnight.EC.Plant.WEB.Models.External.ExternalSpeciesResult c) => new()
    {
        ScientificName = c.ScientificName,
        CommonName = c.CommonName,
        ChineseName = c.ChineseName,
        Genus = c.Genus,
        Family = c.Family,
        ImageUrl = c.ImageUrl,
        IdentificationHint = c.IdentificationHint,
        TaxonId = c.TaxonId,
        Provider = c.Provider,
        SourceType = c.SourceType,
        SourceId = c.SourceId
    };

    private static Midnight.EC.Plant.WEB.Models.External.ExternalSpeciesResult ToExternal(SpeciesCandidateItemViewModel c) => new()
    {
        ScientificName = c.ScientificName,
        CommonName = c.CommonName,
        ChineseName = c.ChineseName,
        Genus = c.Genus,
        Family = c.Family,
        ImageUrl = c.ImageUrl,
        IdentificationHint = c.IdentificationHint,
        TaxonId = c.TaxonId,
        Provider = c.Provider,
        SourceType = c.SourceType,
        SourceId = c.SourceId
    };

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var plant = await _plantService.GetByIdAsync(id, cancellationToken);
        if (plant == null)
        {
            return NotFound();
        }

        var model = new EditPlantViewModel
        {
            Id = plant.Id,
            Name = plant.Name,
            NickName = plant.NickName,
            Location = plant.Location,
            Description = plant.Description,
            StartDate = plant.StartDate?.ToLocalTime().Date,
            SpeciesName = plant.Species?.ChineseName ?? plant.Species?.CommonName,
            SpeciesScientificName = plant.Species?.ScientificName
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditPlantViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _plantService.UpdateAsync(
                id,
                model.Name,
                model.NickName,
                model.Location,
                model.Description,
                model.StartDate,
                cancellationToken);

            TempData["Success"] = "植栽資料已更新。";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update plant failed for {PlantId}", id);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _plantService.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "植栽已刪除。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete plant failed for {PlantId}", id);
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ReselectSpecies(Guid id, CancellationToken cancellationToken)
    {
        var plant = await _plantService.GetByIdAsync(id, cancellationToken);
        if (plant == null)
        {
            return NotFound();
        }

        var chineseName = plant.Species?.ChineseName
            ?? plant.Name
            ?? string.Empty;
        var displayName = !string.IsNullOrWhiteSpace(plant.NickName) ? plant.NickName! : plant.Name;

        var model = new ReselectSpeciesViewModel
        {
            PlantId = id,
            DisplayName = displayName,
            ChineseName = chineseName,
            Candidates = [],
            SelectedIndex = null,
            AvailablePhotos = await LoadPhotoPicksAsync(id, cancellationToken)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchReselectByScientificName(ReselectSpeciesViewModel model, CancellationToken cancellationToken)
    {
        if (model.PlantId == Guid.Empty)
        {
            return RedirectToAction(nameof(Index));
        }

        model.AvailablePhotos = await LoadPhotoPicksAsync(model.PlantId, cancellationToken);

        var query = !string.IsNullOrWhiteSpace(model.ChineseName)
            ? model.ChineseName.Trim()
            : model.ManualScientificName?.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            ModelState.AddModelError(nameof(model.ChineseName), "請輸入中文名。");
            model.Candidates ??= [];
            return View("ReselectSpecies", model);
        }

        // 進階學名優先（若有填）
        if (!string.IsNullOrWhiteSpace(model.ManualScientificName))
        {
            query = model.ManualScientificName.Trim();
        }

        try
        {
            var candidates = await _plantService.SearchSpeciesCandidatesAsync(query, cancellationToken);
            model.Candidates = candidates.Select(c =>
            {
                var item = MapCandidate(c);
                if (string.IsNullOrWhiteSpace(item.ChineseName) && CareGuideJson.HasCjk(model.ChineseName))
                {
                    item.ChineseName = model.ChineseName.Trim();
                }

                return item;
            }).ToList();
            model.SelectedIndex = model.Candidates.Count > 0 ? 0 : null;
            if (model.Candidates.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "查無候選，可改寫中文名或進階學名再試。");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reselect search failed");
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Candidates = [];
        }

        return View("ReselectSpecies", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchReselectByPhoto(
        ReselectSpeciesViewModel model,
        IFormFile? photoUpload,
        CancellationToken cancellationToken)
    {
        if (model.PlantId == Guid.Empty)
        {
            return RedirectToAction(nameof(Index));
        }

        model.AvailablePhotos = await LoadPhotoPicksAsync(model.PlantId, cancellationToken);

        try
        {
            Stream? stream = null;
            string fileName = "plant.jpg";
            string? imageUrlForCards = null;

            if (photoUpload is { Length: > 0 })
            {
                stream = photoUpload.OpenReadStream();
                fileName = photoUpload.FileName;
            }
            else if (model.SelectedPhotoId is Guid photoId)
            {
                var photos = await _imageService.GetByPlantIdAsync(model.PlantId, cancellationToken);
                var photo = photos.FirstOrDefault(p => p.Id == photoId);
                if (photo == null)
                {
                    ModelState.AddModelError(string.Empty, "找不到選定的照片。");
                    model.Candidates ??= [];
                    return View("ReselectSpecies", model);
                }

                var absolute = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photo.StoragePath);
                if (!System.IO.File.Exists(absolute))
                {
                    ModelState.AddModelError(string.Empty, "照片檔案不存在。");
                    model.Candidates ??= [];
                    return View("ReselectSpecies", model);
                }

                stream = System.IO.File.OpenRead(absolute);
                fileName = photo.OriginalFileName ?? photo.FileName;
                imageUrlForCards = _imageStorageService.GetPublicPath(photo.StoragePath);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "請上傳照片或選擇既有照片。");
                model.Candidates ??= [];
                return View("ReselectSpecies", model);
            }

            await using (stream)
            {
                var candidates = await _plantService.SearchSpeciesCandidatesByImageAsync(
                    stream,
                    fileName,
                    string.IsNullOrWhiteSpace(model.ChineseName) ? null : model.ChineseName.Trim(),
                    cancellationToken);

                model.Candidates = candidates.Select(c =>
                {
                    var item = MapCandidate(c);
                    if (string.IsNullOrWhiteSpace(item.ChineseName) && CareGuideJson.HasCjk(model.ChineseName))
                    {
                        item.ChineseName = model.ChineseName.Trim();
                    }

                    if (string.IsNullOrWhiteSpace(item.ImageUrl) && !string.IsNullOrWhiteSpace(imageUrlForCards))
                    {
                        item.ImageUrl = imageUrlForCards;
                    }

                    return item;
                }).ToList();
            }

            model.SelectedIndex = model.Candidates.Count > 0 ? 0 : null;
            if (model.Candidates.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "照片無法辨識出候選，請換圖或改用中文找種。");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reselect photo search failed");
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Candidates = [];
        }

        return View("ReselectSpecies", model);
    }

    private async Task<List<PlantPhotoPickItemViewModel>> LoadPhotoPicksAsync(Guid plantId, CancellationToken cancellationToken)
    {
        var photos = await _imageService.GetByPlantIdAsync(plantId, cancellationToken);
        return photos
            .OrderByDescending(p => p.IsCover)
            .ThenByDescending(p => p.CreateDate)
            .Select(p => new PlantPhotoPickItemViewModel
            {
                Id = p.Id,
                Url = _imageStorageService.GetPublicPath(p.StoragePath),
                IsCover = p.IsCover,
                Note = p.Note
            })
            .ToList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmReselectSpecies(ReselectSpeciesViewModel model, CancellationToken cancellationToken)
    {
        if (model.PlantId == Guid.Empty)
        {
            return RedirectToAction(nameof(Index));
        }

        if (model.SelectedIndex is null ||
            model.Candidates == null ||
            model.SelectedIndex < 0 ||
            model.SelectedIndex >= model.Candidates.Count)
        {
            ModelState.AddModelError(string.Empty, "請選擇一筆物種。");
            model.AvailablePhotos = await LoadPhotoPicksAsync(model.PlantId, cancellationToken);
            return View("ReselectSpecies", model);
        }

        var selected = model.Candidates[model.SelectedIndex.Value];
        var external = ToExternal(selected);

        try
        {
            // 先確保知識以便比對落差（失敗則不換綁）
            var speciesId = await _plantService.EnsureSpeciesKnowledgeAsync(
                external,
                string.IsNullOrWhiteSpace(model.ChineseName) ? selected.ScientificName : model.ChineseName,
                cancellationToken);

            var warnings = await BuildPlantMismatchWarningsAsync(model.PlantId, speciesId, cancellationToken);
            if (warnings.Count > 0 && !model.AcknowledgeMismatch)
            {
                model.MismatchWarnings = warnings;
                model.AvailablePhotos = await LoadPhotoPicksAsync(model.PlantId, cancellationToken);
                ModelState.AddModelError(nameof(model.AcknowledgeMismatch), "實際環境與建議有落差，請勾選「我知道環境不理想」後再確認。");
                return View("ReselectSpecies", model);
            }

            await _plantService.RebindSpeciesAsync(
                model.PlantId,
                external,
                model.ChineseName,
                cancellationToken);

            if (warnings.Count > 0)
            {
                var profile = await _profileService.GetByPlantIdAsync(model.PlantId, cancellationToken);
                if (profile != null)
                {
                    profile.EnvironmentMismatchAcknowledged = true;
                    await _profileService.SaveAsync(model.PlantId, profile, cancellationToken);
                }
            }

            var envFit = await _knowledgeService.RefreshEnvironmentAdviceAsync(model.PlantId, cancellationToken);
            await _reminderService.SyncRemindersAsync(model.PlantId, cancellationToken);

            TempData["Success"] = envFit.Succeeded
                ? "已換綁物種，並同步知識與環境適配建議。"
                : envFit.SkippedNoEnvironment
                    ? "已換綁物種並同步知識。"
                    : string.IsNullOrWhiteSpace(envFit.FailureReason)
                        ? "已換綁物種並同步知識。"
                        : $"已換綁物種並同步知識；環境適配建議失敗：{envFit.FailureReason}";

            return RedirectToAction(nameof(Details), new { id = model.PlantId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmReselectSpecies failed for {PlantId}", model.PlantId);
            ModelState.AddModelError(string.Empty, ex.Message);
            model.AvailablePhotos = await LoadPhotoPicksAsync(model.PlantId, cancellationToken);
            return View("ReselectSpecies", model);
        }
    }

    private async Task<List<string>> BuildPlantMismatchWarningsAsync(
        Guid plantId,
        Guid speciesId,
        CancellationToken cancellationToken)
    {
        var knowledge = await _knowledgeService.GetBySpeciesIdAsync(speciesId, cancellationToken);
        var profile = await _profileService.GetByPlantIdAsync(plantId, cancellationToken);
        if (knowledge == null || profile == null)
        {
            return [];
        }

        var suggestedLight = knowledge.SuggestedLight
            ?? LightLevelDisplay.TryParseFromText(knowledge.LightRequirement);
        var taboos = CareConstraintExtractor.ExtractTaboos(
            knowledge.LightRequirement,
            knowledge.WaterRequirement,
            knowledge.SoilRequirement,
            knowledge.CareSummary,
            knowledge.ExternalCareGuide);

        return CareConstraintExtractor.BuildMismatchWarnings(
            suggestedLight,
            profile.ActualLight,
            taboos,
            profile.HasRainCover,
            profile.ActualPlacement,
            profile.SubstrateType);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var plant = await _plantService.GetByIdAsync(id, cancellationToken);
        if (plant == null)
        {
            return NotFound();
        }

        await _reminderService.SyncRemindersAsync(id, cancellationToken);

        var photos = await _imageService.GetByPlantIdAsync(id, cancellationToken);
        var cover = await _imageService.GetCoverAsync(id, cancellationToken);
        var analyses = await _analysisService.GetByPlantIdAsync(id, cancellationToken);
        var analysesByImage = analyses
            .Where(a => a.ImageId.HasValue)
            .GroupBy(a => a.ImageId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreateDate).First());
        var trend = await _careService.GetTrendAsync(id, 30, cancellationToken);
        var profile = await _profileService.GetByPlantIdAsync(id, cancellationToken);
        var reminders = await _reminderService.GetActiveByPlantIdAsync(id, cancellationToken);
        var timeline = await _timelineService.GetTimelineAsync(id, 90, cancellationToken);
        var knowledgeVm = MapKnowledge(
            plant.Knowledge,
            plant.Species?.ChineseName ?? plant.Name ?? plant.Species?.ScientificName);
        var latestAnalysis = analyses.OrderByDescending(a => a.CreateDate).FirstOrDefault();
        var defaultKeyword = plant.Species?.ChineseName ?? plant.Name ?? plant.Species?.ScientificName ?? string.Empty;

        var model = new PlantDetailViewModel
        {
            Id = plant.Id,
            Name = plant.Name,
            NickName = plant.NickName,
            DisplayName = !string.IsNullOrWhiteSpace(plant.NickName) ? plant.NickName! : plant.Name,
            Location = plant.Location,
            Description = plant.Description,
            StartDate = plant.StartDate,
            SpeciesName = plant.Species?.ChineseName ?? plant.Species?.CommonName ?? plant.Species?.ScientificName,
            SpeciesChineseName = plant.Species?.ChineseName,
            SpeciesScientificName = plant.Species?.ScientificName,
            CoverImagePath = cover != null ? _imageStorageService.GetPublicPath(cover.StoragePath) : null,
            LatestHealthScore = latestAnalysis?.HealthScore,
            Knowledge = knowledgeVm,
            CareSuggestions = BuildCareSuggestions(plant.Knowledge, latestAnalysis?.ResultJson),
            SyncKnowledge = new SyncKnowledgeViewModel { SpeciesKeyword = defaultKeyword },
            Photos = photos.Select(p =>
            {
                analysesByImage.TryGetValue(p.Id, out var photoAnalysis);
                return new PlantPhotoItemViewModel
                {
                    Id = p.Id,
                    PublicPath = _imageStorageService.GetPublicPath(p.StoragePath),
                    Note = p.Note,
                    IsCover = p.IsCover,
                    CreatedAt = p.CreateDate,
                    LatestAnalysis = photoAnalysis == null ? null : MapAnalysis(photoAnalysis)
                };
            }).ToList(),
            Analyses = analyses.Select((a, index) => MapAnalysis(a, index == 0)).ToList(),
            Timeline = timeline.Select(e => new PlantTimelineItemViewModel
            {
                EventType = e.EventType,
                EventDate = e.EventDate,
                Title = e.Title,
                Summary = e.Summary,
                HealthScore = e.HealthScore
            }).ToList(),
            Trend = MapTrend(trend),
            DaysSinceLastWatering = trend.LastWateringDate.HasValue
                ? (DateTime.UtcNow.Date - trend.LastWateringDate.Value.Date).Days
                : null,
            Profile = MapProfile(profile),
            Reminders = reminders.Select(MapReminder).ToList(),
            NewPhoto = new CreatePhotoViewModel(),
            LogEntry = new TodayLogViewModel { LogDate = DateTime.Today }
        };

        ViewBag.CareGuideSectionOrder = await _careGuideLayoutService.GetAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncKnowledge(Guid id, [Bind(Prefix = "SyncKnowledge")] SyncKnowledgeViewModel model, CancellationToken cancellationToken)
    {
        var plant = await _plantService.GetByIdAsync(id, cancellationToken);
        if (plant == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(model.SpeciesKeyword))
        {
            TempData["Error"] = "請輸入物種搜尋關鍵字。";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            var refresh = await _knowledgeService.RefreshFromExternalAsync(plant.SpeciesId, model.SpeciesKeyword.Trim(), cancellationToken);
            var envFit = await _knowledgeService.RefreshEnvironmentAdviceAsync(id, cancellationToken);

            await _reminderService.SyncRemindersAsync(id, cancellationToken);
            var remainingGaps = CareKnowledgeCompleteness.ListMissingFields(refresh.Knowledge);
            ApplySyncKnowledgeFlash(refresh.AiSupplement, refresh.AiFailureReason, envFit, remainingGaps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync knowledge failed for plant {PlantId}", id);
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private void ApplySyncKnowledgeFlash(
        Midnight.EC.Plant.WEB.Models.External.AiSupplementOutcome aiSupplement,
        string? aiFailureReason,
        Midnight.EC.Plant.WEB.Models.External.EnvironmentFitResult envFit,
        IReadOnlyList<string>? remainingGaps = null)
    {
        var gapsText = remainingGaps is { Count: > 0 }
            ? string.Join("、", remainingGaps)
            : null;

        switch (aiSupplement)
        {
            case Midnight.EC.Plant.WEB.Models.External.AiSupplementOutcome.Applied:
                TempData["Success"] = "已重新產生物種照護知識。";
                break;
            case Midnight.EC.Plant.WEB.Models.External.AiSupplementOutcome.AppliedWithRemainingGaps:
                TempData["Success"] = "已重新產生照護知識。";
                TempData["Warning"] = string.IsNullOrWhiteSpace(gapsText)
                    ? "仍有欄位空缺，可再試一次。"
                    : $"仍缺：{gapsText}";
                break;
            case Midnight.EC.Plant.WEB.Models.External.AiSupplementOutcome.ServiceFailed:
                TempData["Error"] = string.IsNullOrWhiteSpace(aiFailureReason)
                    ? "重新產生照護知識失敗。"
                    : aiFailureReason;
                break;
            default:
                TempData["Success"] = "照護知識已更新。";
                if (!string.IsNullOrWhiteSpace(gapsText))
                {
                    TempData["Warning"] = $"仍缺：{gapsText}";
                }
                break;
        }

        if (envFit.Succeeded)
        {
            TempData["Success"] = $"{TempData["Success"]} 已依你的實際環境產出適配建議。";
        }
        else if (envFit.SkippedNoEnvironment)
        {
            TempData["Flash"] ??= "尚未填寫實際環境；補上後可再同步，AI 才會對照陽台／日照／遮雨／介質分析。";
        }
        else if (!string.IsNullOrWhiteSpace(envFit.FailureReason))
        {
            TempData["Warning"] = string.IsNullOrWhiteSpace(TempData["Warning"] as string)
                ? $"物種知識已更新，但「針對實際環境」分析失敗：{envFit.FailureReason}"
                : $"{TempData["Warning"]}；環境適配分析失敗：{envFit.FailureReason}";
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditEnvironment(Guid id, CancellationToken cancellationToken)
    {
        var plant = await _plantService.GetByIdAsync(id, cancellationToken);
        if (plant == null)
        {
            return NotFound();
        }

        var profile = await _profileService.GetByPlantIdAsync(id, cancellationToken);
        var knowledge = plant.SpeciesId != Guid.Empty
            ? await _knowledgeService.GetBySpeciesIdAsync(plant.SpeciesId, cancellationToken)
            : null;

        var model = MapEnvironmentForm(id, plant.NickName ?? plant.Name, profile, knowledge);
        ViewBag.PlantId = id;
        ViewBag.DisplayName = plant.NickName ?? plant.Name;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEnvironment(Guid id, PlantProfileViewModel model, CancellationToken cancellationToken)
    {
        var plant = await _plantService.GetByIdAsync(id, cancellationToken);
        if (plant == null)
        {
            return NotFound();
        }

        var existing = await _profileService.GetByPlantIdAsync(id, cancellationToken);
        var knowledge = await _knowledgeService.GetBySpeciesIdAsync(plant.SpeciesId, cancellationToken);
        var suggestedLight = existing?.OverrideSuggestedLight
            ?? (knowledge != null ? LightLevelDisplay.TryParseFromText(knowledge.LightRequirement) : null);
        // Prefer DB SuggestedLight after migration; fallback parse
        var taboos = CareConstraintExtractor.FromJson(existing?.OverrideCareTaboosJson);
        if (taboos.Count == 0 && knowledge != null)
        {
            taboos = CareConstraintExtractor.ExtractTaboos(
                knowledge.LightRequirement,
                knowledge.WaterRequirement,
                knowledge.SoilRequirement,
                knowledge.CareSummary,
                knowledge.ExternalCareGuide);
        }

        var warnings = CareConstraintExtractor.BuildMismatchWarnings(
            suggestedLight,
            model.ActualLight,
            taboos,
            model.HasRainCover,
            model.ActualPlacement,
            model.SubstrateType);

        if (warnings.Count > 0 && !model.AcknowledgeMismatch)
        {
            model.MismatchWarnings = warnings;
            model.SuggestedLight = suggestedLight;
            model.CareTaboos = taboos;
            ModelState.AddModelError(nameof(model.AcknowledgeMismatch), "實際環境與建議有落差，請勾選「我知道環境不理想」後再儲存。");
            ViewBag.PlantId = id;
            ViewBag.DisplayName = plant.NickName ?? plant.Name;
            return View(model);
        }

        await _profileService.SaveAsync(id, new PlantProfileDto
        {
            WateringIntervalDays = existing?.WateringIntervalDays ?? PlantService.DefaultWateringIntervalDays,
            FertilizingIntervalDays = existing?.FertilizingIntervalDays,
            TargetHumidityMin = existing?.TargetHumidityMin,
            TargetHumidityMax = existing?.TargetHumidityMax,
            TargetTemperatureMin = existing?.TargetTemperatureMin,
            TargetTemperatureMax = existing?.TargetTemperatureMax,
            PersonalCareNotes = existing?.PersonalCareNotes,
            ActualPlacement = model.ActualPlacement,
            ActualLight = model.ActualLight,
            HasRainCover = model.HasRainCover,
            SubstrateType = model.SubstrateType,
            SaucerState = model.SaucerState,
            City = model.City,
            OverrideSuggestedLight = existing?.OverrideSuggestedLight,
            OverrideCareTaboosJson = existing?.OverrideCareTaboosJson,
            WateringIntervalDetachedFromWiki = existing?.WateringIntervalDetachedFromWiki ?? false,
            EnvironmentMismatchAcknowledged = warnings.Count > 0,
            AiEnvironmentAdvice = existing?.AiEnvironmentAdvice
        }, cancellationToken);

        var environmentChanged = HasEnvironmentAdviceInputsChanged(existing, model);
        if (!environmentChanged)
        {
            TempData["Success"] = "實際環境已儲存（參數未變更，未重跑 AI 適配建議）。";
            return RedirectToAction(nameof(Details), new { id });
        }

        var envFit = await _knowledgeService.RefreshEnvironmentAdviceAsync(id, cancellationToken);
        TempData["Success"] = envFit.Succeeded
            ? "實際環境已儲存，並已依環境更新 AI 適配建議。"
            : envFit.SkippedNoEnvironment
                ? "實際環境已儲存。"
                : string.IsNullOrWhiteSpace(envFit.FailureReason)
                    ? "實際環境已儲存。"
                    : $"實際環境已儲存，但 AI 適配建議更新失敗：{envFit.FailureReason}";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProfile(Guid id, PlantProfileViewModel model, CancellationToken cancellationToken)
    {
        var existing = await _profileService.GetByPlantIdAsync(id, cancellationToken);
        await _profileService.SaveAsync(id, new PlantProfileDto
        {
            WateringIntervalDays = model.WateringIntervalDays,
            FertilizingIntervalDays = model.FertilizingIntervalDays,
            TargetHumidityMin = model.TargetHumidityMin,
            TargetHumidityMax = model.TargetHumidityMax,
            TargetTemperatureMin = model.TargetTemperatureMin,
            TargetTemperatureMax = model.TargetTemperatureMax,
            PersonalCareNotes = model.PersonalCareNotes,
            ActualPlacement = existing?.ActualPlacement,
            ActualLight = existing?.ActualLight,
            HasRainCover = existing?.HasRainCover,
            SubstrateType = existing?.SubstrateType,
            SaucerState = existing?.SaucerState,
            City = existing?.City,
            OverrideSuggestedLight = existing?.OverrideSuggestedLight,
            OverrideCareTaboosJson = existing?.OverrideCareTaboosJson,
            WateringIntervalDetachedFromWiki = existing?.WateringIntervalDetachedFromWiki ?? false,
            EnvironmentMismatchAcknowledged = existing?.EnvironmentMismatchAcknowledged ?? false
        }, cancellationToken);

        await _reminderService.SyncRemindersAsync(id, cancellationToken);
        TempData["Success"] = "個人化設定已儲存。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissReminder(Guid id, Guid reminderId, CancellationToken cancellationToken)
    {
        await _reminderService.DismissAsync(reminderId, cancellationToken);
        TempData["Success"] = "提醒已略過。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(Guid id, [Bind(Prefix = "NewPhoto")] CreatePhotoViewModel model, IFormFile? photoFile, CancellationToken cancellationToken)
    {
        if (photoFile == null || photoFile.Length == 0)
        {
            TempData["Error"] = "請選擇要上傳的照片。";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var stream = photoFile.OpenReadStream();
        await _imageService.UploadAsync(
            id,
            stream,
            photoFile.FileName,
            photoFile.ContentType,
            model.Note,
            model.SetAsCover,
            cancellationToken);

        TempData["Success"] = model.SetAsCover ? "照片已上傳並設為封面。" : "照片已上傳。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCoverPhoto(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        await _imageService.SetCoverAsync(id, imageId, cancellationToken);
        TempData["Success"] = "已更新封面照片。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePhotoNote(Guid id, Guid imageId, UpdatePhotoNoteViewModel model, CancellationToken cancellationToken)
    {
        await _imageService.UpdateNoteAsync(imageId, model.Note, cancellationToken);
        TempData["Success"] = "照片備註已更新。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        await _imageService.DeleteAsync(imageId, cancellationToken);
        TempData["Success"] = "照片已刪除。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AnalyzePhoto(Guid id, Guid imageId, CancellationToken cancellationToken = default)
    {
        var job = await _analysisService.StartPhotoAnalysisAsync(id, imageId, cancellationToken);
        return AcceptedAtAction(nameof(AnalysisStatus), new { id, jobId = job.Id }, job);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCareRecord(Guid id, CreateCareRecordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "照護紀錄資料不完整。";
            return RedirectToAction(nameof(Details), new { id });
        }

        var unit = string.IsNullOrWhiteSpace(model.Unit) ? DefaultUnit(model.CareType) : model.Unit;
        await _careService.CreateAsync(id, model.RecordDate, model.CareType, model.NumericValue, unit, model.Note, cancellationToken);
        await _reminderService.SyncRemindersAsync(id, cancellationToken);
        TempData["Success"] = "照護紀錄已新增。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLogEntry(
        Guid id,
        [Bind(Prefix = "LogEntry")] TodayLogViewModel model,
        List<IFormFile>? photos,
        CancellationToken cancellationToken)
    {
        if (model.LogDate is null)
        {
            TempData["Error"] = "請選擇日期。";
            return RedirectToAction(nameof(Details), new { id });
        }

        var day = model.LogDate.Value.Date;
        if (day > DateTime.Today)
        {
            TempData["Error"] = "日期不能選未來。";
            return RedirectToAction(nameof(Details), new { id });
        }

        return await SaveLogInternalAsync(id, day, model, photos, stayOnDetails: true, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTodayLog(
        Guid id,
        [Bind(Prefix = "TodayLog")] TodayLogViewModel model,
        List<IFormFile>? photos,
        CancellationToken cancellationToken)
    {
        model.LogDate ??= DateTime.Today;
        if (model.LogDate.Value.Date > DateTime.Today)
        {
            TempData["Error"] = "日期不能選未來。";
            return RedirectToAction(nameof(Details), new { id });
        }

        return await SaveLogInternalAsync(id, model.LogDate.Value.Date, model, photos, stayOnDetails: true, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BackfillLog(
        Guid id,
        [Bind(Prefix = "BackfillLog")] TodayLogViewModel model,
        List<IFormFile>? photos,
        CancellationToken cancellationToken)
    {
        if (model.LogDate is null)
        {
            TempData["Error"] = "請選擇日期。";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (model.LogDate.Value.Date > DateTime.Today)
        {
            TempData["Error"] = "日期不能選未來。";
            return RedirectToAction(nameof(Details), new { id });
        }

        return await SaveLogInternalAsync(id, model.LogDate.Value.Date, model, photos, stayOnDetails: true, cancellationToken);
    }

    private async Task<IActionResult> SaveLogInternalAsync(
        Guid id,
        DateTime day,
        TodayLogViewModel model,
        List<IFormFile>? photos,
        bool stayOnDetails,
        CancellationToken cancellationToken)
    {
        if (!model.Watered && !model.Fertilized && string.IsNullOrWhiteSpace(model.Note) && (photos == null || photos.Count == 0))
        {
            TempData["Error"] = "請至少勾選澆水／施肥，或寫一句話／上傳照片。";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            if (model.Watered)
            {
                await _careService.CreateAsync(id, day, CareRecordType.Watering, null, null, null, cancellationToken);
            }

            if (model.Fertilized)
            {
                await _careService.CreateAsync(id, day, CareRecordType.Fertilizing, null, null, null, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(model.Note))
            {
                await _diaryService.CreateAsync(id, day, null, model.Note.Trim(), cancellationToken);
            }

            if (photos != null)
            {
                foreach (var photo in photos.Where(p => p.Length > 0))
                {
                    await using var stream = photo.OpenReadStream();
                    await _imageService.UploadAsync(id, stream, photo.FileName, photo.ContentType, null, false, cancellationToken);
                }
            }

            if (model.Watered)
            {
                await _reminderService.SyncRemindersAsync(id, cancellationToken);
            }

            TempData["Success"] = day.Date == DateTime.Today ? "今日紀錄已儲存。" : $"已記上 {day:yyyy/MM/dd}。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save log failed for plant {PlantId}", id);
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(Guid id, AnalysisScope scope = AnalysisScope.Recent30Days, CancellationToken cancellationToken = default)
    {
        var job = await _analysisService.StartAnalysisAsync(id, null, scope, cancellationToken);
        return AcceptedAtAction(nameof(AnalysisStatus), new { id, jobId = job.Id }, job);
    }

    [HttpGet]
    public async Task<IActionResult> AnalysisStatus(Guid id, Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _analysisService.GetJobStatusAsync(jobId, cancellationToken);
        if (job == null || job.PlantId != id)
        {
            return NotFound();
        }

        return Json(new AnalysisStatusViewModel
        {
            JobId = job.Id,
            Status = job.Status,
            AnalysisId = job.AnalysisId,
            ErrorMessage = job.ErrorMessage
        });
    }

    private static PlantAnalysisItemViewModel MapAnalysis(PlantAnalysisDto analysis, bool isExpanded = false)
    {
        string? fertilizerText = null;
        if (!string.IsNullOrWhiteSpace(analysis.ResultJson))
        {
            var parsed = JsonHelper.Deserialize<PlantAnalysisResultDto>(analysis.ResultJson);
            if (parsed?.FertilizerAdvice != null)
            {
                fertilizerText = CareGuideJson.FormatFertilizerRequirement(parsed.FertilizerAdvice);
            }
        }

        return new PlantAnalysisItemViewModel
        {
            Id = analysis.Id,
            ImageId = analysis.ImageId,
            CreatedAt = analysis.CreateDate,
            Summary = analysis.Summary,
            HealthScore = analysis.HealthScore,
            Confidence = analysis.Confidence,
            ResultJson = analysis.ResultJson,
            FertilizerAdviceText = fertilizerText,
            IsExpanded = isExpanded
        };
    }

    private static PlantKnowledgeViewModel? MapKnowledge(PlantKnowledgeDto? knowledge, string? plantDisplayName = null)
    {
        if (knowledge == null)
        {
            return new PlantKnowledgeViewModel
            {
                MissingFields = ["光照", "建議日照", "澆水", "濕度", "溫度下限", "溫度上限", "土壤", "施肥", "生長季", "結構化照護指南"],
                IsSparse = true
            };
        }

        var externalCareGuide = ExternalCareGuideBuilder.SanitizeForDisplay(knowledge.ExternalCareGuide);
        if (string.IsNullOrWhiteSpace(externalCareGuide) && !string.IsNullOrWhiteSpace(plantDisplayName))
        {
            externalCareGuide = ExternalCareGuideBuilder.Build(plantDisplayName, new ExternalKnowledgeResult
            {
                LightRequirement = knowledge.LightRequirement,
                WaterRequirement = knowledge.WaterRequirement,
                HumidityRequirement = knowledge.HumidityRequirement,
                TemperatureMin = knowledge.TemperatureMin,
                TemperatureMax = knowledge.TemperatureMax,
                SoilRequirement = knowledge.SoilRequirement,
                FertilizerRequirement = knowledge.FertilizerRequirement,
                GrowthSeason = knowledge.GrowthSeason
            });
        }

        externalCareGuide = ExternalCareGuideBuilder.SanitizeForDisplay(externalCareGuide);

        var suggestedLight = CareKnowledgeCompleteness.ResolveSuggestedLight(
            knowledge.SuggestedLight,
            knowledge.LightRequirement,
            knowledge.CareSummary,
            knowledge.ExternalCareGuide);

        var vm = new PlantKnowledgeViewModel
        {
            LightRequirement = knowledge.LightRequirement,
            WaterRequirement = knowledge.WaterRequirement,
            HumidityRequirement = knowledge.HumidityRequirement,
            TemperatureRange = FormatTemperature(knowledge.TemperatureMin, knowledge.TemperatureMax),
            SoilRequirement = knowledge.SoilRequirement,
            FertilizerRequirement = knowledge.FertilizerRequirement,
            GrowthSeason = knowledge.GrowthSeason,
            CareSummary = knowledge.CareSummary,
            ExternalCareGuide = externalCareGuide,
            SuggestedLight = suggestedLight,
            MissingFields = CareKnowledgeCompleteness.ListMissingFields(knowledge).ToList()
        };

        vm.IsSparse = string.IsNullOrWhiteSpace(vm.LightRequirement)
            && string.IsNullOrWhiteSpace(vm.WaterRequirement)
            && string.IsNullOrWhiteSpace(vm.HumidityRequirement)
            && string.IsNullOrWhiteSpace(vm.TemperatureRange);

        return vm;
    }

    private static PlantCareSuggestionsViewModel BuildCareSuggestions(PlantKnowledgeDto? knowledge, string? latestResultJson)
    {
        var suggestions = new PlantCareSuggestionsViewModel();

        if (knowledge != null)
        {
            suggestions.SuggestedWateringIntervalDays = InferWateringIntervalDays(knowledge.WaterRequirement);
            ParseHumidityRange(knowledge.HumidityRequirement, out var hMin, out var hMax);
            suggestions.SuggestedHumidityMin = hMin;
            suggestions.SuggestedHumidityMax = hMax;
            suggestions.SuggestedTemperatureMin = knowledge.TemperatureMin;
            suggestions.SuggestedTemperatureMax = knowledge.TemperatureMax;

            var hints = new List<string>();
            if (!string.IsNullOrWhiteSpace(knowledge.LightRequirement))
            {
                hints.Add($"光照：{knowledge.LightRequirement}");
            }

            if (!string.IsNullOrWhiteSpace(knowledge.WaterRequirement))
            {
                hints.Add($"澆水：{knowledge.WaterRequirement}");
            }

            suggestions.PersonalCareNotesHint = hints.Count == 0 ? null : string.Join("；", hints);
        }

        if (!string.IsNullOrWhiteSpace(latestResultJson))
        {
            var ai = JsonHelper.Deserialize<PlantAnalysisResultDto>(latestResultJson);
            suggestions.AiWateringAdvice = ai?.WateringAdvice;
            suggestions.AiGrowthTrend = ai?.GrowthTrend;
            if (ai?.FertilizerAdvice != null)
            {
                suggestions.AiFertilizerAdvice = CareGuideJson.FormatFertilizerRequirement(ai.FertilizerAdvice);
            }
        }

        return suggestions;
    }

    private static int? InferWateringIntervalDays(string? waterRequirement)
    {
        if (string.IsNullOrWhiteSpace(waterRequirement))
        {
            return null;
        }

        if (waterRequirement.Contains("乾", StringComparison.Ordinal) ||
            waterRequirement.Contains("耐旱", StringComparison.Ordinal) ||
            waterRequirement.Contains("少水", StringComparison.Ordinal))
        {
            return 10;
        }

        if (waterRequirement.Contains("中等", StringComparison.Ordinal))
        {
            return 7;
        }

        if (waterRequirement.Contains("頻繁", StringComparison.Ordinal) ||
            waterRequirement.Contains("濕", StringComparison.Ordinal))
        {
            return 3;
        }

        return 7;
    }

    private static void ParseHumidityRange(string? text, out decimal? min, out decimal? max)
    {
        min = null;
        max = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var numbers = System.Text.RegularExpressions.Regex.Matches(text, @"(\d+)");
        if (numbers.Count >= 2)
        {
            min = decimal.Parse(numbers[0].Value);
            max = decimal.Parse(numbers[1].Value);
        }
        else if (numbers.Count == 1)
        {
            max = decimal.Parse(numbers[0].Value);
        }
    }

    private static PlantReminderItemViewModel MapReminder(PlantReminderDto r) => new()
    {
        Id = r.Id,
        PlantId = r.PlantId,
        PlantName = r.PlantName,
        ReminderType = r.ReminderType,
        Priority = r.Priority,
        Title = r.Title,
        Message = r.Message,
        DueDate = r.DueDate,
        IsOverdue = r.IsOverdue
    };

    private static PlantProfileViewModel MapProfile(PlantProfileDto? profile) => profile == null
        ? new PlantProfileViewModel()
        : new PlantProfileViewModel
        {
            WateringIntervalDays = profile.WateringIntervalDays,
            FertilizingIntervalDays = profile.FertilizingIntervalDays,
            TargetHumidityMin = profile.TargetHumidityMin,
            TargetHumidityMax = profile.TargetHumidityMax,
            TargetTemperatureMin = profile.TargetTemperatureMin,
            TargetTemperatureMax = profile.TargetTemperatureMax,
            PersonalCareNotes = profile.PersonalCareNotes,
            ActualPlacement = profile.ActualPlacement,
            ActualLight = profile.ActualLight,
            HasRainCover = profile.HasRainCover,
            SubstrateType = profile.SubstrateType,
            SaucerState = profile.SaucerState,
            City = profile.City,
            AiEnvironmentAdvice = profile.AiEnvironmentAdvice
        };

    private static bool HasEnvironmentAdviceInputsChanged(PlantProfileDto? existing, PlantProfileViewModel model)
    {
        if (existing == null)
        {
            return model.ActualPlacement != null
                || model.ActualLight != null
                || model.HasRainCover != null
                || !string.IsNullOrWhiteSpace(model.SubstrateType)
                || model.SaucerState != null
                || !string.IsNullOrWhiteSpace(model.City);
        }

        return existing.ActualPlacement != model.ActualPlacement
            || existing.ActualLight != model.ActualLight
            || existing.HasRainCover != model.HasRainCover
            || !string.Equals(existing.SubstrateType?.Trim(), model.SubstrateType?.Trim(), StringComparison.Ordinal)
            || existing.SaucerState != model.SaucerState
            || !string.Equals(existing.City?.Trim(), model.City?.Trim(), StringComparison.Ordinal);
    }

    private static PlantProfileViewModel MapEnvironmentForm(
        Guid plantId,
        string displayName,
        PlantProfileDto? profile,
        PlantKnowledgeDto? knowledge)
    {
        var suggested = profile?.OverrideSuggestedLight
            ?? LightLevelDisplay.TryParseFromText(knowledge?.LightRequirement);
        var taboos = CareConstraintExtractor.FromJson(profile?.OverrideCareTaboosJson);
        if (taboos.Count == 0)
        {
            taboos = CareConstraintExtractor.ExtractTaboos(
                knowledge?.LightRequirement,
                knowledge?.WaterRequirement,
                knowledge?.SoilRequirement,
                knowledge?.CareSummary,
                knowledge?.ExternalCareGuide);
        }

        var model = MapProfile(profile);
        model.SuggestedLight = suggested;
        model.CareTaboos = taboos;
        model.EnvironmentIncomplete =
            profile?.ActualPlacement == null ||
            profile.ActualLight == null ||
            profile.HasRainCover == null ||
            string.IsNullOrWhiteSpace(profile.SubstrateType) ||
            profile.SaucerState == null ||
            string.IsNullOrWhiteSpace(profile.City);
        return model;
    }

    private static PlantTrendViewModel MapTrend(PlantTrendDto trend) => new()
    {
        LabelsJson = JsonSerializer.Serialize(trend.Labels),
        HealthScoresJson = JsonSerializer.Serialize(trend.HealthScores),
        TemperaturesJson = JsonSerializer.Serialize(trend.Temperatures),
        HumiditiesJson = JsonSerializer.Serialize(trend.Humidities),
        LightLevelsJson = JsonSerializer.Serialize(trend.LightLevels),
        WateringCountsJson = JsonSerializer.Serialize(trend.WateringCounts),
        LastWateringDate = trend.LastWateringDate,
        TotalWateringCount = trend.TotalWateringCount
    };

    private static string FormatCareDisplay(CareRecordType type, decimal? value, string? unit, string? note)
    {
        if (value.HasValue)
        {
            return $"{value}{unit}";
        }

        return note ?? type.GetDisplayName();
    }

    private static string? DefaultUnit(CareRecordType type) => type switch
    {
        CareRecordType.Watering => "ml",
        CareRecordType.Light => "lux",
        CareRecordType.Temperature => "°C",
        CareRecordType.Humidity => "%",
        _ => null
    };

    private static string? FormatTemperature(decimal? min, decimal? max)
    {
        if (min == null && max == null)
        {
            return null;
        }

        return $"{min}°C ~ {max}°C";
    }
}

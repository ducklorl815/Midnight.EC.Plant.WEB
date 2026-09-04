using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Midnight.EC.Plant.WEB.Models.AI;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Services.External;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Services.Plant;
using Midnight.EC.Plant.WEB.Utility.Json;
using Midnight.EC.Plant.WEB.ViewModels;

namespace Midnight.EC.Plant.WEB.Controllers;

public class PlantController : Controller
{
    private readonly IPlantService _plantService;
    private readonly IPlantDiaryService _diaryService;
    private readonly IPlantImageService _imageService;
    private readonly IPlantAnalysisService _analysisService;
    private readonly IPlantCareService _careService;
    private readonly IPlantProfileService _profileService;
    private readonly IPlantReminderService _reminderService;
    private readonly IPlantTimelineService _timelineService;
    private readonly IPlantKnowledgeService _knowledgeService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<PlantController> _logger;

    public PlantController(
        IPlantService plantService,
        IPlantDiaryService diaryService,
        IPlantImageService imageService,
        IPlantAnalysisService analysisService,
        IPlantCareService careService,
        IPlantProfileService profileService,
        IPlantReminderService reminderService,
        IPlantTimelineService timelineService,
        IPlantKnowledgeService knowledgeService,
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
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var dashboard = await _reminderService.GetDashboardAsync(cancellationToken);
        var cards = dashboard.Select(MapDashboardCard).ToList();

        var overdue = cards
            .Where(p => p.IsWateringOverdue)
            .OrderByDescending(p => p.DaysSinceLastWatering ?? 0)
            .ToList();
        var missingDate = cards
            .Where(p => p.MissingLastWateringDate)
            .OrderBy(p => p.DisplayName)
            .ToList();
        var incomplete = cards
            .Where(p => !p.MissingLastWateringDate && (p.KnowledgeIncomplete || p.EnvironmentIncomplete))
            .OrderBy(p => p.DisplayName)
            .ToList();

        var model = new PlantListViewModel
        {
            Plants = cards,
            OverdueWatering = overdue,
            MissingWateringDate = missingDate,
            IncompleteData = incomplete,
            TotalActiveReminders = dashboard.Sum(p => p.ActiveReminderCount),
            TotalOverdueReminders = overdue.Count
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchWater(int[]? plantIds, CancellationToken cancellationToken)
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
    public async Task<IActionResult> SetLastWateringDate(int plantId, DateTime lastWateringDate, CancellationToken cancellationToken)
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

        if (model.SelectedIndex is null ||
            model.Candidates == null ||
            model.SelectedIndex < 0 ||
            model.SelectedIndex >= model.Candidates.Count)
        {
            ModelState.AddModelError(string.Empty, "請選擇一筆物種。");
            return View(model);
        }

        var selected = model.Candidates[model.SelectedIndex.Value];
        try
        {
            var plant = await _plantService.CreateFromDraftAsync(
                ToDraft(model.Draft),
                ToExternal(selected),
                cancellationToken);

            TempData["Flash"] = "植物已建立。提醒週期預設 7 天；若知識有建議週期，可之後在單盆確認是否套用。";
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
    public async Task<IActionResult> SearchByScientificName(ConfirmSpeciesViewModel model, CancellationToken cancellationToken)
    {
        if (model.Draft == null)
        {
            return RedirectToAction(nameof(Create));
        }

        if (string.IsNullOrWhiteSpace(model.ManualScientificName))
        {
            ModelState.AddModelError(nameof(model.ManualScientificName), "請輸入學名。");
            model.Candidates ??= [];
            return View("ConfirmSpecies", model);
        }

        IReadOnlyList<Midnight.EC.Plant.WEB.Models.External.ExternalSpeciesResult> candidates;
        try
        {
            candidates = await _plantService.SearchSpeciesCandidatesAsync(model.ManualScientificName.Trim(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Manual scientific search failed");
            candidates = [];
        }

        model.Candidates = candidates.Select(MapCandidate).ToList();
        model.SelectedIndex = model.Candidates.Count > 0 ? 0 : null;
        if (model.Candidates.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "學名查無候選，可改寫或先略過建檔。");
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

        try
        {
            var plant = await _plantService.CreateFromDraftAsync(ToDraft(model.Draft), null, cancellationToken);
            TempData["Flash"] = "已先建檔（物種未確認）。可之後補知識。";
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
        Location = model.Location,
        Description = model.Description,
        StartDate = model.StartDate,
        WateredToday = model.WateredToday
    };

    private static SpeciesCandidateItemViewModel MapCandidate(Midnight.EC.Plant.WEB.Models.External.ExternalSpeciesResult c) => new()
    {
        ScientificName = c.ScientificName,
        CommonName = c.CommonName,
        ChineseName = c.ChineseName,
        Genus = c.Genus,
        Family = c.Family,
        ImageUrl = c.ImageUrl,
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
        TaxonId = c.TaxonId,
        Provider = c.Provider,
        SourceType = c.SourceType,
        SourceId = c.SourceId
    };

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
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
    public async Task<IActionResult> Edit(int id, EditPlantViewModel model, CancellationToken cancellationToken)
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
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
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
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
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
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreatedAt).First());
        var careRecords = await _careService.GetByPlantIdAsync(id, cancellationToken);
        var trend = await _careService.GetTrendAsync(id, 30, cancellationToken);
        var profile = await _profileService.GetByPlantIdAsync(id, cancellationToken);
        var reminders = await _reminderService.GetActiveByPlantIdAsync(id, cancellationToken);
        var knowledgeVm = MapKnowledge(
            plant.Knowledge,
            plant.Species?.ChineseName ?? plant.Name ?? plant.Species?.ScientificName);
        var latestAnalysis = analyses.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
        var defaultKeyword = plant.Species?.ChineseName ?? plant.Name ?? plant.Species?.ScientificName ?? string.Empty;

        if (knowledgeVm != null && knowledgeVm.IsSparse && !string.IsNullOrWhiteSpace(defaultKeyword))
        {
            try
            {
                await _knowledgeService.RefreshFromExternalAsync(plant.SpeciesId, defaultKeyword.Trim(), cancellationToken);
                plant = await _plantService.GetByIdAsync(id, cancellationToken) ?? plant;
                knowledgeVm = MapKnowledge(
                    plant.Knowledge,
                    plant.Species?.ChineseName ?? plant.Name ?? plant.Species?.ScientificName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto refresh knowledge failed for plant {PlantId}", id);
            }
        }

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
                    CreatedAt = p.CreatedAt,
                    LatestAnalysis = photoAnalysis == null ? null : MapAnalysis(photoAnalysis)
                };
            }).ToList(),
            Analyses = analyses.Select((a, index) => MapAnalysis(a, index == 0)).ToList(),
            CareRecords = careRecords.Select(r => new PlantCareRecordItemViewModel
            {
                Id = r.Id,
                RecordDate = r.RecordDate,
                CareType = r.CareType,
                NumericValue = r.NumericValue,
                Unit = r.Unit,
                Note = r.Note,
                DisplayValue = FormatCareDisplay(r.CareType, r.NumericValue, r.Unit, r.Note)
            }).ToList(),
            Trend = MapTrend(trend),
            DaysSinceLastWatering = trend.LastWateringDate.HasValue
                ? (DateTime.UtcNow.Date - trend.LastWateringDate.Value.Date).Days
                : null,
            Profile = MapProfile(profile),
            Reminders = reminders.Select(MapReminder).ToList(),
            NewCareRecord = new CreateCareRecordViewModel(),
            NewPhoto = new CreatePhotoViewModel(),
            TodayLog = new TodayLogViewModel(),
            BackfillLog = new TodayLogViewModel { LogDate = DateTime.Today.AddDays(-1) }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncKnowledge(int id, [Bind(Prefix = "SyncKnowledge")] SyncKnowledgeViewModel model, CancellationToken cancellationToken)
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
            await _knowledgeService.RefreshFromExternalAsync(plant.SpeciesId, model.SpeciesKeyword.Trim(), cancellationToken);

            await _reminderService.SyncRemindersAsync(id, cancellationToken);
            TempData["Success"] = "已從外部 API（Trefle / iNaturalist / GBIF / Wikipedia / AI）更新植物知識。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync knowledge failed for plant {PlantId}", id);
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> EditEnvironment(int id, CancellationToken cancellationToken)
    {
        var plant = await _plantService.GetByIdAsync(id, cancellationToken);
        if (plant == null)
        {
            return NotFound();
        }

        var profile = await _profileService.GetByPlantIdAsync(id, cancellationToken);
        var knowledge = plant.SpeciesId > 0
            ? await _knowledgeService.GetBySpeciesIdAsync(plant.SpeciesId, cancellationToken)
            : null;

        var model = MapEnvironmentForm(id, plant.NickName ?? plant.Name, profile, knowledge);
        ViewBag.PlantId = id;
        ViewBag.DisplayName = plant.NickName ?? plant.Name;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEnvironment(int id, PlantProfileViewModel model, CancellationToken cancellationToken)
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
            City = model.City,
            OverrideSuggestedLight = existing?.OverrideSuggestedLight,
            OverrideCareTaboosJson = existing?.OverrideCareTaboosJson,
            WateringIntervalDetachedFromWiki = existing?.WateringIntervalDetachedFromWiki ?? false,
            EnvironmentMismatchAcknowledged = warnings.Count > 0
        }, cancellationToken);

        TempData["Success"] = "實際環境已儲存。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProfile(int id, PlantProfileViewModel model, CancellationToken cancellationToken)
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
    public async Task<IActionResult> DismissReminder(int id, int reminderId, CancellationToken cancellationToken)
    {
        await _reminderService.DismissAsync(reminderId, cancellationToken);
        TempData["Success"] = "提醒已略過。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(int id, [Bind(Prefix = "NewPhoto")] CreatePhotoViewModel model, IFormFile? photoFile, CancellationToken cancellationToken)
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
    public async Task<IActionResult> SetCoverPhoto(int id, int imageId, CancellationToken cancellationToken)
    {
        await _imageService.SetCoverAsync(id, imageId, cancellationToken);
        TempData["Success"] = "已更新封面照片。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePhotoNote(int id, int imageId, UpdatePhotoNoteViewModel model, CancellationToken cancellationToken)
    {
        await _imageService.UpdateNoteAsync(imageId, model.Note, cancellationToken);
        TempData["Success"] = "照片備註已更新。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int id, int imageId, CancellationToken cancellationToken)
    {
        await _imageService.DeleteAsync(imageId, cancellationToken);
        TempData["Success"] = "照片已刪除。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AnalyzePhoto(int id, int imageId, CancellationToken cancellationToken = default)
    {
        var job = await _analysisService.StartPhotoAnalysisAsync(id, imageId, cancellationToken);
        return AcceptedAtAction(nameof(AnalysisStatus), new { id, jobId = job.Id }, job);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCareRecord(int id, CreateCareRecordViewModel model, CancellationToken cancellationToken)
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
    public async Task<IActionResult> SaveTodayLog(
        int id,
        [Bind(Prefix = "TodayLog")] TodayLogViewModel model,
        List<IFormFile>? photos,
        CancellationToken cancellationToken)
    {
        return await SaveLogInternalAsync(id, DateTime.Today, model, photos, stayOnDetails: true, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BackfillLog(
        int id,
        [Bind(Prefix = "BackfillLog")] TodayLogViewModel model,
        List<IFormFile>? photos,
        CancellationToken cancellationToken)
    {
        if (model.LogDate is null)
        {
            TempData["Error"] = "請選擇補記日期。";
            return RedirectToAction(nameof(Details), new { id });
        }

        var day = model.LogDate.Value.Date;
        if (day >= DateTime.Today)
        {
            TempData["Error"] = "補記只能選今天以前的日期；今天請用上方極簡列。";
            return RedirectToAction(nameof(Details), new { id });
        }

        return await SaveLogInternalAsync(id, day, model, photos, stayOnDetails: true, cancellationToken);
    }

    private async Task<IActionResult> SaveLogInternalAsync(
        int id,
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

            TempData["Success"] = day.Date == DateTime.Today ? "今日紀錄已儲存。" : $"已補記 {day:yyyy/MM/dd}。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save log failed for plant {PlantId}", id);
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(int id, AnalysisScope scope = AnalysisScope.Recent30Days, CancellationToken cancellationToken = default)
    {
        var job = await _analysisService.StartAnalysisAsync(id, null, scope, cancellationToken);
        return AcceptedAtAction(nameof(AnalysisStatus), new { id, jobId = job.Id }, job);
    }

    [HttpGet]
    public async Task<IActionResult> AnalysisStatus(int id, int jobId, CancellationToken cancellationToken)
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

    private static PlantAnalysisItemViewModel MapAnalysis(PlantAnalysisDto analysis, bool isExpanded = false) => new()
    {
        Id = analysis.Id,
        ImageId = analysis.ImageId,
        CreatedAt = analysis.CreatedAt,
        Summary = analysis.Summary,
        HealthScore = analysis.HealthScore,
        Confidence = analysis.Confidence,
        ResultJson = analysis.ResultJson,
        IsExpanded = isExpanded
    };

    private static PlantKnowledgeViewModel? MapKnowledge(PlantKnowledgeDto? knowledge, string? plantDisplayName = null)
    {
        if (knowledge == null)
        {
            return null;
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

        var vm = new PlantKnowledgeViewModel
        {
            LightRequirement = knowledge.LightRequirement,
            WaterRequirement = knowledge.WaterRequirement,
            HumidityRequirement = knowledge.HumidityRequirement,
            TemperatureRange = FormatTemperature(knowledge.TemperatureMin, knowledge.TemperatureMax),
            SoilRequirement = knowledge.SoilRequirement,
            FertilizerRequirement = knowledge.FertilizerRequirement,
            CareSummary = knowledge.CareSummary,
            ExternalCareGuide = externalCareGuide
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
            City = profile.City
        };

    private static PlantProfileViewModel MapEnvironmentForm(
        int plantId,
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

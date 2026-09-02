using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Midnight.EC.Plant.WEB.Models.AI;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Services.External;
using Midnight.EC.Plant.WEB.Services.Interfaces;
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
        var model = new PlantListViewModel
        {
            Plants = dashboard.Select(p => new PlantDashboardCardViewModel
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
                LatestHealthScore = p.LatestHealthScore,
                ActiveReminderCount = p.ActiveReminderCount,
                OverdueReminderCount = p.OverdueReminderCount,
                DaysSinceLastWatering = p.DaysSinceLastWatering,
                TopReminders = p.TopReminders.Select(MapReminder).ToList()
            }).ToList(),
            TotalActiveReminders = dashboard.Sum(p => p.ActiveReminderCount),
            TotalOverdueReminders = dashboard.Sum(p => p.OverdueReminderCount)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreatePlantViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePlantViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var plant = await _plantService.CreateAsync(
                model.Name,
                model.SpeciesKeyword,
                model.NickName,
                model.Location,
                model.Description,
                model.StartDate,
                cancellationToken);

            return RedirectToAction(nameof(Details), new { id = plant.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create plant failed");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

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
            NewPhoto = new CreatePhotoViewModel()
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProfile(int id, PlantProfileViewModel model, CancellationToken cancellationToken)
    {
        await _profileService.SaveAsync(id, new PlantProfileDto
        {
            WateringIntervalDays = model.WateringIntervalDays,
            FertilizingIntervalDays = model.FertilizingIntervalDays,
            TargetHumidityMin = model.TargetHumidityMin,
            TargetHumidityMax = model.TargetHumidityMax,
            TargetTemperatureMin = model.TargetTemperatureMin,
            TargetTemperatureMax = model.TargetTemperatureMax,
            PersonalCareNotes = model.PersonalCareNotes
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
            PersonalCareNotes = profile.PersonalCareNotes
        };

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

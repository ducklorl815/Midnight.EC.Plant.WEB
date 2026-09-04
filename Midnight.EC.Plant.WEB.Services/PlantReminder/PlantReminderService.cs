using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.AI;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Utility.Json;

namespace Midnight.EC.Plant.WEB.Services.PlantReminder;

public class PlantReminderService : IPlantReminderService
{
    private const int DefaultWateringIntervalDays = 7;
    private const int DefaultFertilizingIntervalDays = 30;
    private const int AnalysisSuggestIntervalDays = 14;

    private readonly IPlantReminderRepository _reminderRepository;
    private readonly IPlantRepository _plantRepository;
    private readonly IPlantProfileRepository _profileRepository;
    private readonly IPlantCareRecordRepository _careRepository;
    private readonly IPlantAnalysisRepository _analysisRepository;
    private readonly IPlantImageRepository _imageRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<PlantReminderService> _logger;

    public PlantReminderService(
        IPlantReminderRepository reminderRepository,
        IPlantRepository plantRepository,
        IPlantProfileRepository profileRepository,
        IPlantCareRecordRepository careRepository,
        IPlantAnalysisRepository analysisRepository,
        IPlantImageRepository imageRepository,
        IImageStorageService imageStorageService,
        ILogger<PlantReminderService> logger)
    {
        _reminderRepository = reminderRepository;
        _plantRepository = plantRepository;
        _profileRepository = profileRepository;
        _careRepository = careRepository;
        _analysisRepository = analysisRepository;
        _imageRepository = imageRepository;
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    public async Task<List<PlantReminderDto>> GetActiveByPlantIdAsync(int plantId, CancellationToken cancellationToken = default)
    {
        var reminders = await _reminderRepository.GetActiveByPlantIdAsync(plantId, cancellationToken);
        return reminders.Select(r => r.ToDto()).ToList();
    }

    public async Task<List<PlantDashboardItemDto>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await SyncRemindersAsync(null, cancellationToken);

        var plants = await _plantRepository.GetAllActiveAsync(cancellationToken);
        if (plants.Count == 0)
        {
            return [];
        }

        var plantIds = plants.Select(p => p.Id).ToList();
        var reminders = await _reminderRepository.GetActiveForPlantsAsync(plantIds, cancellationToken);
        var analyses = await LoadLatestAnalysesAsync(plantIds, cancellationToken);
        var covers = await LoadCoverPathsAsync(plantIds, cancellationToken);
        var latestVisual = await LoadLatestVisualActivityAsync(plantIds, cancellationToken);
        var lastWateringDates = await _careRepository.GetLastWateringDatesAsync(plantIds, cancellationToken);
        var today = DateTime.Today;

        var items = new List<PlantDashboardItemDto>();
        foreach (var plant in plants)
        {
            var plantReminders = reminders.Where(r => r.PlantId == plant.Id).ToList();
            lastWateringDates.TryGetValue(plant.Id, out var lastWateringDate);
            var hasWatering = lastWateringDates.ContainsKey(plant.Id);
            int? daysSinceWatering = hasWatering
                ? (today - lastWateringDate.Date).Days
                : null;
            var chineseName = plant.Species?.ChineseName;
            var scientificName = plant.Species?.ScientificName;
            var profile = await _profileRepository.GetByPlantIdAsync(plant.Id, cancellationToken);
            var interval = profile?.WateringIntervalDays
                ?? InferWateringIntervalDays(plant.Species?.Knowledge?.WaterRequirement);
            var isOverdue = hasWatering && daysSinceWatering.HasValue && daysSinceWatering.Value >= interval;
            var knowledge = plant.Species?.Knowledge;
            var suggestedLight = profile?.OverrideSuggestedLight ?? knowledge?.SuggestedLight
                ?? LightLevelDisplay.TryParseFromText(knowledge?.LightRequirement);
            var knowledgeIncomplete = knowledge == null
                || suggestedLight == null
                || string.Equals(scientificName, "未確認", StringComparison.Ordinal);
            var environmentIncomplete =
                profile == null
                || profile.ActualPlacement == null
                || profile.ActualLight == null
                || profile.HasRainCover == null
                || string.IsNullOrWhiteSpace(profile.SubstrateType)
                || string.IsNullOrWhiteSpace(profile.City);

            items.Add(new PlantDashboardItemDto
            {
                Id = plant.Id,
                Name = plant.Name,
                NickName = plant.NickName,
                DisplayName = !string.IsNullOrWhiteSpace(plant.NickName) ? plant.NickName! : plant.Name,
                SpeciesName = chineseName ?? plant.Species?.CommonName ?? scientificName,
                SpeciesChineseName = chineseName,
                SpeciesScientificName = scientificName,
                Location = plant.Location,
                CoverImagePath = covers.GetValueOrDefault(plant.Id),
                LatestVisualActivityAt = latestVisual.TryGetValue(plant.Id, out var visualAt) ? visualAt : null,
                LatestHealthScore = analyses.GetValueOrDefault(plant.Id)?.HealthScore,
                ActiveReminderCount = plantReminders.Count,
                OverdueReminderCount = plantReminders.Count(r => r.DueDate.Date < today),
                TopReminders = plantReminders.Take(3).Select(r => r.ToDto(plant.Name)).ToList(),
                LastWateringDate = hasWatering ? lastWateringDate : null,
                DaysSinceLastWatering = daysSinceWatering,
                WateringIntervalDays = interval,
                IsWateringOverdue = isOverdue,
                MissingLastWateringDate = !hasWatering,
                KnowledgeIncomplete = knowledgeIncomplete,
                EnvironmentIncomplete = environmentIncomplete,
                ActualLightLabel = profile?.ActualLight is { } light
                    ? LightLevelDisplay.ToLabel(light)
                    : null
            });
        }

        return items;
    }

    public async Task SyncRemindersAsync(int? plantId, CancellationToken cancellationToken = default)
    {
        var plants = plantId.HasValue
            ? await LoadSinglePlantAsync(plantId.Value, cancellationToken)
            : await _plantRepository.GetAllActiveAsync(cancellationToken);

        foreach (var plant in plants)
        {
            var profile = await _profileRepository.GetByPlantIdAsync(plant.Id, cancellationToken);
            var careRecords = await _careRepository.GetByPlantIdAsync(plant.Id, cancellationToken);
            var analyses = await _analysisRepository.GetByPlantIdAsync(plant.Id, cancellationToken);

            await UpsertCareReminderAsync(
                plant,
                ReminderType.Watering,
                $"watering-{plant.Id}",
                careRecords.Where(r => r.CareType == CareRecordType.Watering).OrderByDescending(r => r.RecordDate).FirstOrDefault()?.RecordDate,
                profile?.WateringIntervalDays ?? InferWateringIntervalDays(plant.Species?.Knowledge?.WaterRequirement),
                "澆水提醒",
                "依個人化設定，這盆植物可能需要澆水了。",
                cancellationToken);

            var fertilizerHint = plant.Species?.Knowledge?.FertilizerRequirement;
            var fertilizingMessage = string.IsNullOrWhiteSpace(fertilizerHint)
                ? "建議檢查是否需要施肥。"
                : $"建議檢查是否需要施肥。官方建議：{fertilizerHint}";

            await UpsertCareReminderAsync(
                plant,
                ReminderType.Fertilizing,
                $"fertilizing-{plant.Id}",
                careRecords.Where(r => r.CareType == CareRecordType.Fertilizing).OrderByDescending(r => r.RecordDate).FirstOrDefault()?.RecordDate,
                profile?.FertilizingIntervalDays ?? DefaultFertilizingIntervalDays,
                "施肥提醒",
                fertilizingMessage,
                cancellationToken);

            await UpsertAnalysisReminderAsync(plant, analyses, cancellationToken);
            await SyncAiAlertRemindersAsync(plant, analyses, cancellationToken);
        }

        await _reminderRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Synced reminders for {Count} plant(s)", plants.Count);
    }

    public async Task DismissAsync(int reminderId, CancellationToken cancellationToken = default)
    {
        var reminder = await _reminderRepository.GetByIdAsync(reminderId, cancellationToken)
            ?? throw new InvalidOperationException("找不到提醒。");

        reminder.Status = ReminderStatus.Dismissed;
        reminder.DismissedAt = DateTime.UtcNow;
        reminder.UpdatedAt = DateTime.UtcNow;
        await _reminderRepository.UpdateAsync(reminder, cancellationToken);
        await _reminderRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<Midnight.EC.Plant.WEB.Models.Entities.Plant>> LoadSinglePlantAsync(int plantId, CancellationToken cancellationToken)
    {
        var plant = await _plantRepository.GetByIdWithDetailsAsync(plantId, cancellationToken);
        return plant == null ? [] : [plant];
    }

    private async Task UpsertCareReminderAsync(
        Midnight.EC.Plant.WEB.Models.Entities.Plant plant,
        ReminderType type,
        string sourceKey,
        DateTime? lastRecordDate,
        int intervalDays,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var dueDate = (lastRecordDate ?? plant.CreatedAt).Date.AddDays(intervalDays);
        var priority = dueDate.Date < DateTime.UtcNow.Date
            ? ReminderPriority.High
            : dueDate.Date == DateTime.UtcNow.Date
                ? ReminderPriority.Normal
                : ReminderPriority.Low;

        if (dueDate.Date > DateTime.UtcNow.Date.AddDays(2))
        {
            await DeactivateReminderIfExistsAsync(plant.Id, sourceKey, cancellationToken);
            return;
        }

        await UpsertReminderAsync(plant.Id, sourceKey, type, priority, title, message, dueDate, cancellationToken);
    }

    private async Task UpsertAnalysisReminderAsync(
        Midnight.EC.Plant.WEB.Models.Entities.Plant plant,
        List<Midnight.EC.Plant.WEB.Models.Entities.PlantAnalysis> analyses,
        CancellationToken cancellationToken)
    {
        var sourceKey = $"analysis-{plant.Id}";
        var latest = analyses.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
        var dueDate = (latest?.CreatedAt ?? plant.CreatedAt).Date.AddDays(AnalysisSuggestIntervalDays);

        if (dueDate.Date > DateTime.UtcNow.Date)
        {
            await DeactivateReminderIfExistsAsync(plant.Id, sourceKey, cancellationToken);
            return;
        }

        var priority = dueDate.Date < DateTime.UtcNow.Date ? ReminderPriority.Normal : ReminderPriority.Low;
        await UpsertReminderAsync(
            plant.Id,
            sourceKey,
            ReminderType.Analysis,
            priority,
            "AI 分析建議",
            "已一段時間未進行 AI 分析，建議重新評估植物狀態。",
            dueDate,
            cancellationToken);
    }

    private async Task SyncAiAlertRemindersAsync(
        Midnight.EC.Plant.WEB.Models.Entities.Plant plant,
        List<Midnight.EC.Plant.WEB.Models.Entities.PlantAnalysis> analyses,
        CancellationToken cancellationToken)
    {
        var latest = analyses.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
        if (latest?.ResultJson == null)
        {
            return;
        }

        var result = JsonHelper.Deserialize<PlantAnalysisResultDto>(latest.ResultJson);
        if (result?.Alerts == null || result.Alerts.Count == 0)
        {
            return;
        }

        for (var i = 0; i < result.Alerts.Count; i++)
        {
            var alert = result.Alerts[i];
            if (string.IsNullOrWhiteSpace(alert))
            {
                continue;
            }

            var sourceKey = $"ai-alert-{plant.Id}-{i}";
            await UpsertReminderAsync(
                plant.Id,
                sourceKey,
                ReminderType.AiAlert,
                ReminderPriority.High,
                "AI 異常預警",
                alert,
                DateTime.UtcNow.Date,
                cancellationToken);
        }
    }

    private async Task UpsertReminderAsync(
        int plantId,
        string sourceKey,
        ReminderType type,
        ReminderPriority priority,
        string title,
        string? message,
        DateTime dueDate,
        CancellationToken cancellationToken)
    {
        var existing = await _reminderRepository.GetBySourceKeyAsync(plantId, sourceKey, cancellationToken);
        var now = DateTime.UtcNow;

        if (existing == null)
        {
            await _reminderRepository.AddAsync(new Midnight.EC.Plant.WEB.Models.Entities.PlantReminder
            {
                PlantId = plantId,
                ReminderType = type,
                Priority = priority,
                Status = ReminderStatus.Active,
                Title = title,
                Message = message,
                DueDate = dueDate,
                SourceKey = sourceKey,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);
            return;
        }

        if (existing.Status == ReminderStatus.Dismissed)
        {
            return;
        }

        existing.Title = title;
        existing.Message = message;
        existing.DueDate = dueDate;
        existing.Priority = priority;
        existing.ReminderType = type;
        existing.Status = ReminderStatus.Active;
        existing.UpdatedAt = now;
        await _reminderRepository.UpdateAsync(existing, cancellationToken);
    }

    private async Task DeactivateReminderIfExistsAsync(int plantId, string sourceKey, CancellationToken cancellationToken)
    {
        var existing = await _reminderRepository.GetBySourceKeyAsync(plantId, sourceKey, cancellationToken);
        if (existing == null || existing.Status != ReminderStatus.Active)
        {
            return;
        }

        existing.Status = ReminderStatus.Completed;
        existing.UpdatedAt = DateTime.UtcNow;
        await _reminderRepository.UpdateAsync(existing, cancellationToken);
    }

    private async Task<Dictionary<int, Midnight.EC.Plant.WEB.Models.Entities.PlantAnalysis>> LoadLatestAnalysesAsync(
        IEnumerable<int> plantIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, Midnight.EC.Plant.WEB.Models.Entities.PlantAnalysis>();
        foreach (var id in plantIds)
        {
            var analyses = await _analysisRepository.GetByPlantIdAsync(id, cancellationToken);
            var latest = analyses.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
            if (latest != null)
            {
                result[id] = latest;
            }
        }

        return result;
    }

    private async Task<Dictionary<int, string>> LoadCoverPathsAsync(IEnumerable<int> plantIds, CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, string>();
        foreach (var id in plantIds)
        {
            var cover = await _imageRepository.GetCoverByPlantIdAsync(id, cancellationToken);
            if (cover != null)
            {
                result[id] = _imageStorageService.GetPublicPath(cover.StoragePath);
            }
        }

        return result;
    }

    private async Task<Dictionary<int, DateTime>> LoadLatestVisualActivityAsync(
        IEnumerable<int> plantIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, DateTime>();
        foreach (var id in plantIds)
        {
            var images = await _imageRepository.GetByPlantIdAsync(id, cancellationToken);
            var latest = images.OrderByDescending(i => i.CreatedAt).FirstOrDefault();
            if (latest != null)
            {
                result[id] = latest.CreatedAt;
            }
        }

        return result;
    }

    private static int InferWateringIntervalDays(string? waterRequirement)
    {
        if (string.IsNullOrWhiteSpace(waterRequirement))
        {
            return DefaultWateringIntervalDays;
        }

        var match = Regex.Match(waterRequirement, @"(\d+)\s*(天|日|day)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var days))
        {
            return Math.Clamp(days, 1, 30);
        }

        if (waterRequirement.Contains("週", StringComparison.Ordinal) ||
            waterRequirement.Contains("week", StringComparison.OrdinalIgnoreCase))
        {
            return 7;
        }

        if (waterRequirement.Contains("月", StringComparison.Ordinal) ||
            waterRequirement.Contains("month", StringComparison.OrdinalIgnoreCase))
        {
            return 30;
        }

        return DefaultWateringIntervalDays;
    }
}

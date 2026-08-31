using Midnight.EC.Plant.WEB.Models.AI;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Utility.Json;

namespace Midnight.EC.Plant.WEB.Services.PlantComparison;

public class PlantComparisonService : IPlantComparisonService
{
    private readonly IPlantRepository _plantRepository;
    private readonly IPlantCareService _careService;
    private readonly IPlantAnalysisRepository _analysisRepository;
    private readonly IPlantReminderRepository _reminderRepository;

    public PlantComparisonService(
        IPlantRepository plantRepository,
        IPlantCareService careService,
        IPlantAnalysisRepository analysisRepository,
        IPlantReminderRepository reminderRepository)
    {
        _plantRepository = plantRepository;
        _careService = careService;
        _analysisRepository = analysisRepository;
        _reminderRepository = reminderRepository;
    }

    public async Task<PlantComparisonResultDto> CompareAsync(IEnumerable<int> plantIds, int days = 30, CancellationToken cancellationToken = default)
    {
        var idList = plantIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new PlantComparisonResultDto();
        }

        var plants = await _plantRepository.GetByIdsWithDetailsAsync(idList, cancellationToken);
        var reminders = await _reminderRepository.GetActiveForPlantsAsync(idList, cancellationToken);
        var result = new PlantComparisonResultDto();

        foreach (var plant in plants)
        {
            var trend = await _careService.GetTrendAsync(plant.Id, days, cancellationToken);
            var analyses = await _analysisRepository.GetByPlantIdAsync(plant.Id, cancellationToken);
            var latest = analyses.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
            var growthTrend = ExtractGrowthTrend(latest?.ResultJson);

            var avgTemp = AverageNonNull(trend.Temperatures);
            var avgHumidity = AverageNonNull(trend.Humidities);

            result.Items.Add(new PlantComparisonItemDto
            {
                PlantId = plant.Id,
                Name = plant.Name,
                SpeciesName = plant.Species?.ChineseName ?? plant.Species?.CommonName ?? plant.Species?.ScientificName,
                Location = plant.Location,
                LatestHealthScore = latest?.HealthScore,
                LastWateringDate = trend.LastWateringDate,
                WateringCount30Days = trend.TotalWateringCount,
                AvgTemperature = avgTemp,
                AvgHumidity = avgHumidity,
                GrowthTrend = growthTrend,
                ActiveReminderCount = reminders.Count(r => r.PlantId == plant.Id)
            });

            result.HealthScoreLabels.Add(plant.Name);
            result.HealthScores.Add(latest?.HealthScore);
        }

        return result;
    }

    private static string? ExtractGrowthTrend(string? resultJson)
    {
        if (string.IsNullOrWhiteSpace(resultJson))
        {
            return null;
        }

        var parsed = JsonHelper.Deserialize<PlantAnalysisResultDto>(resultJson);
        return parsed?.GrowthTrend;
    }

    private static decimal? AverageNonNull(IEnumerable<decimal?> values)
    {
        var list = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return list.Count == 0 ? null : list.Average();
    }
}

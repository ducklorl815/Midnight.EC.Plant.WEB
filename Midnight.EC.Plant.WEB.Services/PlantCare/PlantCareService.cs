using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.PlantCare;

public class PlantCareService : IPlantCareService
{
    private readonly IPlantCareRecordRepository _careRepository;
    private readonly IPlantAnalysisRepository _analysisRepository;
    private readonly IPlantRepository _plantRepository;

    public PlantCareService(
        IPlantCareRecordRepository careRepository,
        IPlantAnalysisRepository analysisRepository,
        IPlantRepository plantRepository)
    {
        _careRepository = careRepository;
        _analysisRepository = analysisRepository;
        _plantRepository = plantRepository;
    }

    public async Task<List<PlantCareRecordDto>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default)
    {
        var records = await _careRepository.GetByPlantIdAsync(plantId, cancellationToken);
        return records.Select(r => r.ToDto()).ToList();
    }

    public async Task<PlantCareRecordDto> CreateAsync(
        int plantId,
        DateTime recordDate,
        CareRecordType careType,
        decimal? numericValue,
        string? unit,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var record = new Midnight.EC.Plant.WEB.Models.Entities.PlantCareRecord
        {
            PlantId = plant.Id,
            RecordDate = recordDate,
            CareType = careType,
            NumericValue = numericValue,
            Unit = unit,
            Note = note,
            CreatedAt = DateTime.UtcNow
        };

        await _careRepository.AddAsync(record, cancellationToken);
        await _careRepository.SaveChangesAsync(cancellationToken);
        return record.ToDto();
    }

    public async Task<PlantTrendDto> GetTrendAsync(int plantId, int days = 30, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days + 1);
        var careRecords = await _careRepository.GetRecentByPlantIdAsync(plantId, since, cancellationToken);
        var analyses = await _analysisRepository.GetByPlantIdAsync(plantId, cancellationToken);
        var recentAnalyses = analyses.Where(a => a.CreatedAt >= since).ToList();

        var trend = new PlantTrendDto();
        for (var i = 0; i < days; i++)
        {
            var date = since.AddDays(i);
            var label = date.ToString("MM/dd");
            trend.Labels.Add(label);

            var dayCare = careRecords.Where(r => r.RecordDate.Date == date.Date).ToList();
            trend.Temperatures.Add(AverageValue(dayCare, CareRecordType.Temperature));
            trend.Humidities.Add(AverageValue(dayCare, CareRecordType.Humidity));
            trend.LightLevels.Add(AverageValue(dayCare, CareRecordType.Light));
            trend.WateringCounts.Add(dayCare.Count(r => r.CareType == CareRecordType.Watering));

            var dayAnalysis = recentAnalyses
                .Where(a => a.CreatedAt.Date == date.Date)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault();
            trend.HealthScores.Add(dayAnalysis?.HealthScore);
        }

        var wateringRecords = careRecords.Where(r => r.CareType == CareRecordType.Watering).ToList();
        trend.TotalWateringCount = wateringRecords.Count;
        trend.LastWateringDate = wateringRecords.OrderByDescending(r => r.RecordDate).FirstOrDefault()?.RecordDate;

        return trend;
    }

    private static decimal? AverageValue(List<Midnight.EC.Plant.WEB.Models.Entities.PlantCareRecord> records, CareRecordType type)
    {
        var values = records.Where(r => r.CareType == type && r.NumericValue.HasValue).Select(r => r.NumericValue!.Value).ToList();
        return values.Count == 0 ? null : values.Average();
    }
}

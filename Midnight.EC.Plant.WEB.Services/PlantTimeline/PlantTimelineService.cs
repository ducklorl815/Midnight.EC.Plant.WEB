using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.PlantTimeline;

public class PlantTimelineService : IPlantTimelineService
{
    private readonly IPlantDiaryRepository _diaryRepository;
    private readonly IPlantCareRecordRepository _careRepository;
    private readonly IPlantAnalysisRepository _analysisRepository;

    public PlantTimelineService(
        IPlantDiaryRepository diaryRepository,
        IPlantCareRecordRepository careRepository,
        IPlantAnalysisRepository analysisRepository)
    {
        _diaryRepository = diaryRepository;
        _careRepository = careRepository;
        _analysisRepository = analysisRepository;
    }

    public async Task<List<PlantTimelineEventDto>> GetTimelineAsync(int plantId, int days = 90, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days + 1);
        var events = new List<PlantTimelineEventDto>();

        var diaries = await _diaryRepository.GetByPlantIdAsync(plantId, cancellationToken);
        events.AddRange(diaries
            .Where(d => d.DiaryDate >= since)
            .Select(d => new PlantTimelineEventDto
            {
                EventType = TimelineEventType.Diary,
                EventDate = d.DiaryDate,
                Title = string.IsNullOrWhiteSpace(d.Title) ? "日記紀錄" : d.Title,
                Summary = d.Note,
                RelatedId = d.Id
            }));

        var careRecords = await _careRepository.GetByPlantIdAsync(plantId, cancellationToken);
        events.AddRange(careRecords
            .Where(r => r.RecordDate >= since)
            .Select(r => new PlantTimelineEventDto
            {
                EventType = TimelineEventType.CareRecord,
                EventDate = r.RecordDate,
                Title = $"{r.CareType} 紀錄",
                Summary = r.NumericValue.HasValue ? $"{r.NumericValue}{r.Unit}" : r.Note,
                RelatedId = r.Id
            }));

        var analyses = await _analysisRepository.GetByPlantIdAsync(plantId, cancellationToken);
        events.AddRange(analyses
            .Where(a => a.CreatedAt >= since)
            .Select(a => new PlantTimelineEventDto
            {
                EventType = TimelineEventType.Analysis,
                EventDate = a.CreatedAt,
                Title = "AI 分析",
                Summary = a.Summary,
                RelatedId = a.Id,
                HealthScore = a.HealthScore
            }));

        return events
            .OrderByDescending(e => e.EventDate)
            .ThenByDescending(e => e.RelatedId)
            .ToList();
    }
}

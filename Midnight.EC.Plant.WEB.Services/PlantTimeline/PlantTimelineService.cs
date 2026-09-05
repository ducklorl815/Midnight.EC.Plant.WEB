using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.PlantTimeline;

public class PlantTimelineService
{
    private readonly PlantDiaryRespo _diaryRepository;
    private readonly PlantCareRecordRespo _careRepository;

    public PlantTimelineService(
        PlantDiaryRespo diaryRepository,
        PlantCareRecordRespo careRepository)
    {
        _diaryRepository = diaryRepository;
        _careRepository = careRepository;
    }

    public async Task<List<PlantTimelineEventDto>> GetTimelineAsync(Guid plantId, int days = 90, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days + 1);
        var events = new List<PlantTimelineEventDto>();

        var diaries = await _diaryRepository.GetByPlantIdAsync(plantId, cancellationToken);
        events.AddRange(diaries
            .Where(d => d.DiaryDate >= since && !string.IsNullOrWhiteSpace(d.Note))
            .Select(d => new PlantTimelineEventDto
            {
                EventType = TimelineEventType.Diary,
                EventDate = d.DiaryDate,
                Title = "備註",
                Summary = d.Note,
                RelatedId = d.Id
            }));

        var careRecords = await _careRepository.GetByPlantIdAsync(plantId, cancellationToken);
        events.AddRange(careRecords
            .Where(r => r.RecordDate >= since
                && (r.CareType == CareRecordType.Watering || r.CareType == CareRecordType.Fertilizing))
            .Select(r => new PlantTimelineEventDto
            {
                EventType = TimelineEventType.CareRecord,
                EventDate = r.RecordDate,
                Title = r.CareType.GetDisplayName(),
                Summary = string.IsNullOrWhiteSpace(r.Note) ? null : r.Note,
                RelatedId = r.Id
            }));

        return events
            .OrderByDescending(e => e.EventDate)
            .ThenByDescending(e => e.RelatedId)
            .ToList();
    }
}

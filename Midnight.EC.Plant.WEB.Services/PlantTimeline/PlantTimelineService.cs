using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Respository;

namespace Midnight.EC.Plant.WEB.Services.PlantTimeline;

public class PlantTimelineService
{
    private readonly PlantDiaryRespo _diaryRepository;
    private readonly PlantImageRespo _imageRepository;

    public PlantTimelineService(
        PlantDiaryRespo diaryRepository,
        PlantImageRespo imageRepository)
    {
        _diaryRepository = diaryRepository;
        _imageRepository = imageRepository;
    }

    public async Task<List<PlantTimelineEventDto>> GetTimelineAsync(Guid plantId, int days = 90, CancellationToken cancellationToken = default)
    {
        _ = days;
        var diaries = await _diaryRepository.GetByPlantIdAsync(plantId, cancellationToken);
        var images = await _imageRepository.GetByPlantIdAsync(plantId, cancellationToken);

        return diaries
            .Select(d =>
            {
                var paths = images
                    .Where(i => i.DiaryID == d.Id)
                    .Select(i => i.StoragePath)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();
                return new PlantTimelineEventDto
                {
                    EventType = TimelineEventType.Diary,
                    EventDate = d.DiaryDate,
                    Title = "記一筆",
                    Summary = string.IsNullOrWhiteSpace(d.Note) ? null : d.Note,
                    RelatedId = d.Id,
                    DiaryId = paths.Count > 0 ? d.Id : null,
                    ImageStoragePaths = paths
                };
            })
            .Where(e => e.ImageStoragePaths.Count > 0)
            .OrderByDescending(e => e.EventDate)
            .ThenByDescending(e => e.RelatedId)
            .ToList();
    }
}

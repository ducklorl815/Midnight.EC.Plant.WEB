using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.PlantDiary;

public class PlantDiaryService : IPlantDiaryService
{
    private readonly IPlantDiaryRepository _diaryRepository;
    private readonly IPlantRepository _plantRepository;

    public PlantDiaryService(IPlantDiaryRepository diaryRepository, IPlantRepository plantRepository)
    {
        _diaryRepository = diaryRepository;
        _plantRepository = plantRepository;
    }

    public async Task<List<PlantDiaryDto>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default)
    {
        var diaries = await _diaryRepository.GetByPlantIdAsync(plantId, cancellationToken);
        return diaries.Select(d => d.ToDto()).ToList();
    }

    public async Task<PlantDiaryDto> CreateAsync(int plantId, DateTime diaryDate, string? title, string? note, CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var now = DateTime.UtcNow;
        var diary = new Midnight.EC.Plant.WEB.Models.Entities.PlantDiary
        {
            PlantId = plant.Id,
            DiaryDate = diaryDate,
            Title = title,
            Note = note,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _diaryRepository.AddAsync(diary, cancellationToken);
        await _diaryRepository.SaveChangesAsync(cancellationToken);

        var created = await _diaryRepository.GetByIdWithImagesAsync(diary.Id, cancellationToken)
            ?? throw new InvalidOperationException("建立日記後無法讀取資料。");

        return created.ToDto();
    }

    public async Task DeleteAsync(int diaryId, CancellationToken cancellationToken = default)
    {
        var diary = await _diaryRepository.GetByIdWithImagesAsync(diaryId, cancellationToken)
            ?? throw new InvalidOperationException("找不到日記。");

        await _diaryRepository.DeleteAsync(diary, cancellationToken);
        await _diaryRepository.SaveChangesAsync(cancellationToken);
    }
}

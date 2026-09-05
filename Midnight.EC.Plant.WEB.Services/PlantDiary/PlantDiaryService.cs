using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.PlantDiary;

public class PlantDiaryService
{
    private readonly PlantDiaryRespo _diaryRepository;
    private readonly PlantRespo _plantRepository;

    public PlantDiaryService(PlantDiaryRespo diaryRepository, PlantRespo plantRepository)
    {
        _diaryRepository = diaryRepository;
        _plantRepository = plantRepository;
    }

    public async Task<List<PlantDiaryDto>> GetByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var diaries = await _diaryRepository.GetByPlantIdAsync(plantId, cancellationToken);
        return diaries.Select(d => d.ToDto()).ToList();
    }

    public async Task<PlantDiaryDto> CreateAsync(Guid plantId, DateTime diaryDate, string? title, string? note, CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var now = DateTime.UtcNow;
        var diary = new Midnight.EC.Plant.WEB.Models.Models.PlantDiaryModel
        {
            PlantId = plant.Id,
            DiaryDate = diaryDate,
            Title = title,
            Note = note,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _diaryRepository.InsertAsync(diary, cancellationToken);
        var created = await _diaryRepository.GetByIdAsync(diary.Id, cancellationToken)
            ?? throw new InvalidOperationException("建立日記後無法讀取資料。");

        return created.ToDto();
    }

    public async Task DeleteAsync(Guid diaryId, CancellationToken cancellationToken = default)
    {
        var diary = await _diaryRepository.GetByIdAsync(diaryId, cancellationToken)
            ?? throw new InvalidOperationException("找不到日記。");

        await _diaryRepository.SoftDeleteAsync(diary.Id, cancellationToken);
    }
}

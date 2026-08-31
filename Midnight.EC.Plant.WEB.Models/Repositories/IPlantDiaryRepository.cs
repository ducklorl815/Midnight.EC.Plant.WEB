using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public interface IPlantDiaryRepository
{
    Task<List<PlantDiary>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task<List<PlantDiary>> GetRecentByPlantIdAsync(int plantId, DateTime since, CancellationToken cancellationToken = default);
    Task<PlantDiary?> GetByIdWithImagesAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(PlantDiary diary, CancellationToken cancellationToken = default);
    Task DeleteAsync(PlantDiary diary, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

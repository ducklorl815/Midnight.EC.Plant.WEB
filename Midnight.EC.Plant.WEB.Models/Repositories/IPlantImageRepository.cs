using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public interface IPlantImageRepository
{
    Task<PlantImage?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<PlantImage>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task<PlantImage?> GetCoverByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task AddAsync(PlantImage image, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlantImage image, CancellationToken cancellationToken = default);
    Task DeleteAsync(PlantImage image, CancellationToken cancellationToken = default);
    Task ClearCoverAsync(int plantId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public interface IPlantProfileRepository
{
    Task<PlantProfile?> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task AddAsync(PlantProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlantProfile profile, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

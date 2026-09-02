using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public interface IPlantCareRecordRepository
{
    Task<List<PlantCareRecord>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task<List<PlantCareRecord>> GetRecentByPlantIdAsync(int plantId, DateTime since, CancellationToken cancellationToken = default);
    Task<Dictionary<int, DateTime>> GetLastWateringDatesAsync(IEnumerable<int> plantIds, CancellationToken cancellationToken = default);
    Task AddAsync(PlantCareRecord record, CancellationToken cancellationToken = default);
    Task DeleteAsync(PlantCareRecord record, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public interface IPlantReminderRepository
{
    Task<List<PlantReminder>> GetActiveByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task<List<PlantReminder>> GetActiveForPlantsAsync(IEnumerable<int> plantIds, CancellationToken cancellationToken = default);
    Task<PlantReminder?> GetBySourceKeyAsync(int plantId, string sourceKey, CancellationToken cancellationToken = default);
    Task<PlantReminder?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(PlantReminder reminder, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlantReminder reminder, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

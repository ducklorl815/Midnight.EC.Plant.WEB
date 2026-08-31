using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public interface IPlantRepository
{
    Task<List<Entities.Plant>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<List<Entities.Plant>> GetByIdsWithDetailsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task<Entities.Plant?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Entities.Plant?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Entities.Plant plant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.Plant plant, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

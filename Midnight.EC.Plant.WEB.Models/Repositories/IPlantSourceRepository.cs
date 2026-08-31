using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public interface IPlantSourceRepository
{
    Task<List<PlantSource>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<PlantSource>> GetBySpeciesIdAsync(int speciesId, CancellationToken cancellationToken = default);
    Task<PlantSource?> GetByIdWithContentsAsync(int id, CancellationToken cancellationToken = default);
    Task<PlantSource?> GetByUrlHashAsync(string contentHash, CancellationToken cancellationToken = default);
    Task AddAsync(PlantSource source, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlantSource source, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

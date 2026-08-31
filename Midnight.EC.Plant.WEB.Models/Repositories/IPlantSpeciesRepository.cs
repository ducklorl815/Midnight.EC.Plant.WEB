using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public interface IPlantSpeciesRepository
{
    Task<List<PlantSpecies>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PlantSpecies?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PlantSpecies?> GetByTaxonIdAsync(string taxonId, CancellationToken cancellationToken = default);
    Task<PlantSpecies?> SearchByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(PlantSpecies species, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlantSpecies species, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

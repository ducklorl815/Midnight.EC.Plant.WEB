using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public interface IPlantKnowledgeRepository
{
    Task<PlantKnowledge?> GetBySpeciesIdAsync(int speciesId, CancellationToken cancellationToken = default);
    Task AddAsync(PlantKnowledge knowledge, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlantKnowledge knowledge, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public interface IPlantAnalysisRepository
{
    Task<List<PlantAnalysis>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task<PlantAnalysis?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(PlantAnalysis analysis, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

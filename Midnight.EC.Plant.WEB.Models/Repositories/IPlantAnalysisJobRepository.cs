using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public interface IPlantAnalysisJobRepository
{
    Task<PlantAnalysisJob?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<PlantAnalysisJob>> GetPendingJobsAsync(int take, CancellationToken cancellationToken = default);
    Task AddAsync(PlantAnalysisJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlantAnalysisJob job, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

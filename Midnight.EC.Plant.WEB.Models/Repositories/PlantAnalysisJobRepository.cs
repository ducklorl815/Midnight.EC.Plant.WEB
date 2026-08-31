using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public class PlantAnalysisJobRepository : IPlantAnalysisJobRepository
{
    private readonly PlantDbContext _context;

    public PlantAnalysisJobRepository(PlantDbContext context)
    {
        _context = context;
    }

    public Task<PlantAnalysisJob?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.PlantAnalysisJobs.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public Task<List<PlantAnalysisJob>> GetPendingJobsAsync(int take, CancellationToken cancellationToken = default) =>
        _context.PlantAnalysisJobs
            .Where(j => j.Status == AnalysisJobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PlantAnalysisJob job, CancellationToken cancellationToken = default) =>
        await _context.PlantAnalysisJobs.AddAsync(job, cancellationToken);

    public Task UpdateAsync(PlantAnalysisJob job, CancellationToken cancellationToken = default)
    {
        _context.PlantAnalysisJobs.Update(job);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public class PlantAnalysisRepository : IPlantAnalysisRepository
{
    private readonly PlantDbContext _context;

    public PlantAnalysisRepository(PlantDbContext context)
    {
        _context = context;
    }

    public Task<List<PlantAnalysis>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default) =>
        _context.PlantAnalyses
            .AsNoTracking()
            .Where(a => a.PlantId == plantId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<PlantAnalysis?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.PlantAnalyses.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AddAsync(PlantAnalysis analysis, CancellationToken cancellationToken = default) =>
        await _context.PlantAnalyses.AddAsync(analysis, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

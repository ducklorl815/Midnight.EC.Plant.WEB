using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public class PlantSourceRepository : IPlantSourceRepository
{
    private readonly PlantDbContext _context;

    public PlantSourceRepository(PlantDbContext context)
    {
        _context = context;
    }

    public Task<List<PlantSource>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.PlantSources
            .AsNoTracking()
            .Include(s => s.Species)
            .Include(s => s.Contents)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<List<PlantSource>> GetBySpeciesIdAsync(int speciesId, CancellationToken cancellationToken = default) =>
        _context.PlantSources
            .AsNoTracking()
            .Include(s => s.Contents)
            .Where(s => s.SpeciesId == speciesId && s.IsActive)
            .ToListAsync(cancellationToken);

    public Task<PlantSource?> GetByIdWithContentsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.PlantSources
            .Include(s => s.Species)
            .Include(s => s.Contents)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<PlantSource?> GetByUrlHashAsync(string contentHash, CancellationToken cancellationToken = default) =>
        _context.PlantSources
            .Include(s => s.Contents)
            .FirstOrDefaultAsync(s => s.ContentHash == contentHash, cancellationToken);

    public async Task AddAsync(PlantSource source, CancellationToken cancellationToken = default) =>
        await _context.PlantSources.AddAsync(source, cancellationToken);

    public Task UpdateAsync(PlantSource source, CancellationToken cancellationToken = default)
    {
        _context.PlantSources.Update(source);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

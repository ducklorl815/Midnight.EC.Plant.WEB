using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public class PlantImageRepository : IPlantImageRepository
{
    private readonly PlantDbContext _context;

    public PlantImageRepository(PlantDbContext context)
    {
        _context = context;
    }

    public Task<PlantImage?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.PlantImages.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<List<PlantImage>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default) =>
        _context.PlantImages
            .AsNoTracking()
            .Where(i => i.PlantId == plantId)
            .OrderByDescending(i => i.IsCover)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<PlantImage?> GetCoverByPlantIdAsync(int plantId, CancellationToken cancellationToken = default) =>
        _context.PlantImages
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.PlantId == plantId && i.IsCover, cancellationToken);

    public async Task AddAsync(PlantImage image, CancellationToken cancellationToken = default) =>
        await _context.PlantImages.AddAsync(image, cancellationToken);

    public Task UpdateAsync(PlantImage image, CancellationToken cancellationToken = default)
    {
        _context.PlantImages.Update(image);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PlantImage image, CancellationToken cancellationToken = default)
    {
        _context.PlantImages.Remove(image);
        return Task.CompletedTask;
    }

    public Task ClearCoverAsync(int plantId, CancellationToken cancellationToken = default) =>
        _context.PlantImages
            .Where(i => i.PlantId == plantId && i.IsCover)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsCover, false), cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

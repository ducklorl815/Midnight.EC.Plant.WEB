using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public class PlantProfileRepository : IPlantProfileRepository
{
    private readonly PlantDbContext _context;

    public PlantProfileRepository(PlantDbContext context)
    {
        _context = context;
    }

    public Task<PlantProfile?> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default) =>
        _context.PlantProfiles.FirstOrDefaultAsync(p => p.PlantId == plantId, cancellationToken);

    public async Task AddAsync(PlantProfile profile, CancellationToken cancellationToken = default) =>
        await _context.PlantProfiles.AddAsync(profile, cancellationToken);

    public Task UpdateAsync(PlantProfile profile, CancellationToken cancellationToken = default)
    {
        _context.PlantProfiles.Update(profile);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

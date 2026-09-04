using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public class PlantRepository : IPlantRepository
{
    private readonly PlantDbContext _context;

    public PlantRepository(PlantDbContext context)
    {
        _context = context;
    }

    public Task<List<Entities.Plant>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        _context.Plants
            .AsNoTracking()
            .Include(p => p.Species)
            .ThenInclude(s => s!.Knowledge)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

    public Task<List<Entities.Plant>> GetByIdsWithDetailsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return _context.Plants
            .AsNoTracking()
            .Include(p => p.Species)
            .ThenInclude(s => s!.Knowledge)
            .Include(p => p.Profile)
            .Where(p => p.IsActive && idList.Contains(p.Id))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Entities.Plant?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Plants.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Entities.Plant?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Plants
            .Include(p => p.Species)
            .ThenInclude(s => s!.Knowledge)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> IsNickNameTakenAsync(string nickName, int? excludePlantId = null, CancellationToken cancellationToken = default)
    {
        var key = nickName.Trim();
        var query = _context.Plants.AsNoTracking().Where(p => p.IsActive && p.NickName != null && p.NickName == key);
        if (excludePlantId.HasValue)
        {
            query = query.Where(p => p.Id != excludePlantId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Entities.Plant plant, CancellationToken cancellationToken = default) =>
        await _context.Plants.AddAsync(plant, cancellationToken);

    public Task UpdateAsync(Entities.Plant plant, CancellationToken cancellationToken = default)
    {
        _context.Plants.Update(plant);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

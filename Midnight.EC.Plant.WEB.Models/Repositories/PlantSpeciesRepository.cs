using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public class PlantSpeciesRepository : IPlantSpeciesRepository
{
    private readonly PlantDbContext _context;

    public PlantSpeciesRepository(PlantDbContext context)
    {
        _context = context;
    }

    public Task<List<PlantSpecies>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.PlantSpecies
            .AsNoTracking()
            .OrderBy(s => s.ChineseName ?? s.CommonName ?? s.ScientificName)
            .ToListAsync(cancellationToken);

    public Task<PlantSpecies?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.PlantSpecies
            .Include(s => s.Knowledge)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<PlantSpecies?> GetByTaxonIdAsync(string taxonId, CancellationToken cancellationToken = default) =>
        _context.PlantSpecies
            .Include(s => s.Knowledge)
            .FirstOrDefaultAsync(s => s.TaxonId == taxonId, cancellationToken);

    public Task<PlantSpecies?> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var keyword = name.Trim();
        return _context.PlantSpecies
            .Include(s => s.Knowledge)
            .Where(s =>
                s.ScientificName.Contains(keyword) ||
                (s.CommonName != null && s.CommonName.Contains(keyword)) ||
                (s.ChineseName != null && s.ChineseName.Contains(keyword)))
            .OrderBy(s => s.ScientificName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(PlantSpecies species, CancellationToken cancellationToken = default) =>
        await _context.PlantSpecies.AddAsync(species, cancellationToken);

    public Task UpdateAsync(PlantSpecies species, CancellationToken cancellationToken = default)
    {
        _context.PlantSpecies.Update(species);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

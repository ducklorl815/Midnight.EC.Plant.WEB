using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public class PlantKnowledgeRepository : IPlantKnowledgeRepository
{
    private readonly PlantDbContext _context;

    public PlantKnowledgeRepository(PlantDbContext context)
    {
        _context = context;
    }

    public Task<PlantKnowledge?> GetBySpeciesIdAsync(int speciesId, CancellationToken cancellationToken = default) =>
        _context.PlantKnowledge.FirstOrDefaultAsync(k => k.SpeciesId == speciesId, cancellationToken);

    public async Task AddAsync(PlantKnowledge knowledge, CancellationToken cancellationToken = default) =>
        await _context.PlantKnowledge.AddAsync(knowledge, cancellationToken);

    public Task UpdateAsync(PlantKnowledge knowledge, CancellationToken cancellationToken = default)
    {
        _context.PlantKnowledge.Update(knowledge);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

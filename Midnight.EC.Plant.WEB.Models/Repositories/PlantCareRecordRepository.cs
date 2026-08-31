using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public class PlantCareRecordRepository : IPlantCareRecordRepository
{
    private readonly PlantDbContext _context;

    public PlantCareRecordRepository(PlantDbContext context)
    {
        _context = context;
    }

    public Task<List<PlantCareRecord>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default) =>
        _context.PlantCareRecords
            .AsNoTracking()
            .Where(r => r.PlantId == plantId)
            .OrderByDescending(r => r.RecordDate)
            .ToListAsync(cancellationToken);

    public Task<List<PlantCareRecord>> GetRecentByPlantIdAsync(int plantId, DateTime since, CancellationToken cancellationToken = default) =>
        _context.PlantCareRecords
            .AsNoTracking()
            .Where(r => r.PlantId == plantId && r.RecordDate >= since)
            .OrderBy(r => r.RecordDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PlantCareRecord record, CancellationToken cancellationToken = default) =>
        await _context.PlantCareRecords.AddAsync(record, cancellationToken);

    public Task DeleteAsync(PlantCareRecord record, CancellationToken cancellationToken = default)
    {
        _context.PlantCareRecords.Remove(record);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

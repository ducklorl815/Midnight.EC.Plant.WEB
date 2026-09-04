using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Enums;

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

    public async Task<Dictionary<int, DateTime>> GetLastWateringDatesAsync(IEnumerable<int> plantIds, CancellationToken cancellationToken = default)
    {
        var idList = plantIds.ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        var records = await _context.PlantCareRecords
            .AsNoTracking()
            .Where(r => idList.Contains(r.PlantId) && r.CareType == CareRecordType.Watering)
            .GroupBy(r => r.PlantId)
            .Select(g => new { PlantId = g.Key, LastDate = g.Max(r => r.RecordDate) })
            .ToListAsync(cancellationToken);

        return records.ToDictionary(r => r.PlantId, r => r.LastDate);
    }

    public Task<PlantCareRecord?> FindSameDayAsync(
        int plantId,
        DateTime recordDate,
        CareRecordType careType,
        CancellationToken cancellationToken = default)
    {
        var day = recordDate.Date;
        var next = day.AddDays(1);
        return _context.PlantCareRecords
            .FirstOrDefaultAsync(
                r => r.PlantId == plantId && r.CareType == careType && r.RecordDate >= day && r.RecordDate < next,
                cancellationToken);
    }

    public async Task AddAsync(PlantCareRecord record, CancellationToken cancellationToken = default) =>
        await _context.PlantCareRecords.AddAsync(record, cancellationToken);

    public Task UpdateAsync(PlantCareRecord record, CancellationToken cancellationToken = default)
    {
        _context.PlantCareRecords.Update(record);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PlantCareRecord record, CancellationToken cancellationToken = default)
    {
        _context.PlantCareRecords.Remove(record);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

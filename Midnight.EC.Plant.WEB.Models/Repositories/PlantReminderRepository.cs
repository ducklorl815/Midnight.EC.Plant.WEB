using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public class PlantReminderRepository : IPlantReminderRepository
{
    private readonly PlantDbContext _context;

    public PlantReminderRepository(PlantDbContext context)
    {
        _context = context;
    }

    public Task<List<PlantReminder>> GetActiveByPlantIdAsync(int plantId, CancellationToken cancellationToken = default) =>
        _context.PlantReminders
            .AsNoTracking()
            .Where(r => r.PlantId == plantId && r.Status == ReminderStatus.Active)
            .OrderBy(r => r.DueDate)
            .ToListAsync(cancellationToken);

    public Task<List<PlantReminder>> GetActiveForPlantsAsync(IEnumerable<int> plantIds, CancellationToken cancellationToken = default)
    {
        var ids = plantIds.ToList();
        return _context.PlantReminders
            .AsNoTracking()
            .Include(r => r.Plant)
            .Where(r => ids.Contains(r.PlantId) && r.Status == ReminderStatus.Active)
            .OrderBy(r => r.DueDate)
            .ToListAsync(cancellationToken);
    }

    public Task<PlantReminder?> GetBySourceKeyAsync(int plantId, string sourceKey, CancellationToken cancellationToken = default) =>
        _context.PlantReminders
            .FirstOrDefaultAsync(r => r.PlantId == plantId && r.SourceKey == sourceKey, cancellationToken);

    public Task<PlantReminder?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.PlantReminders.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task AddAsync(PlantReminder reminder, CancellationToken cancellationToken = default) =>
        await _context.PlantReminders.AddAsync(reminder, cancellationToken);

    public Task UpdateAsync(PlantReminder reminder, CancellationToken cancellationToken = default)
    {
        _context.PlantReminders.Update(reminder);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

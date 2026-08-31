using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Data;
using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Repositories;

public class PlantDiaryRepository : IPlantDiaryRepository
{
    private readonly PlantDbContext _context;

    public PlantDiaryRepository(PlantDbContext context)
    {
        _context = context;
    }

    public Task<List<PlantDiary>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default) =>
        _context.PlantDiaries
            .AsNoTracking()
            .Include(d => d.Images)
            .Where(d => d.PlantId == plantId)
            .OrderByDescending(d => d.DiaryDate)
            .ToListAsync(cancellationToken);

    public Task<List<PlantDiary>> GetRecentByPlantIdAsync(int plantId, DateTime since, CancellationToken cancellationToken = default) =>
        _context.PlantDiaries
            .AsNoTracking()
            .Include(d => d.Images)
            .Where(d => d.PlantId == plantId && d.DiaryDate >= since)
            .OrderByDescending(d => d.DiaryDate)
            .ToListAsync(cancellationToken);

    public Task<PlantDiary?> GetByIdWithImagesAsync(int id, CancellationToken cancellationToken = default) =>
        _context.PlantDiaries
            .Include(d => d.Images)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task AddAsync(PlantDiary diary, CancellationToken cancellationToken = default) =>
        await _context.PlantDiaries.AddAsync(diary, cancellationToken);

    public Task DeleteAsync(PlantDiary diary, CancellationToken cancellationToken = default)
    {
        _context.PlantDiaries.Remove(diary);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

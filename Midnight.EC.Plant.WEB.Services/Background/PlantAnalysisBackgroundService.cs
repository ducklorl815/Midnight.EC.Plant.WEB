using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Services.PlantAnalysis;

namespace Midnight.EC.Plant.WEB.Services.Background;

public class PlantAnalysisBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlantAnalysisBackgroundService> _logger;

    public PlantAnalysisBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PlantAnalysisBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var jobRepository = scope.ServiceProvider.GetRequiredService<Midnight.EC.Plant.WEB.Models.Respository.PlantAnalysisJobRespo>();
                var analysisService = scope.ServiceProvider.GetRequiredService<PlantAnalysisService>();

                var pendingJobs = await jobRepository.GetPendingJobsAsync(5, stoppingToken);
                foreach (var job in pendingJobs)
                {
                    await analysisService.ProcessJobAsync(job.Id, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plant analysis background worker error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}

using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.AI;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.AI;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Utility.Json;

namespace Midnight.EC.Plant.WEB.Services.PlantAnalysis;

public class PlantAnalysisService : IPlantAnalysisService
{
    private readonly IPlantAnalysisRepository _analysisRepository;
    private readonly IPlantAnalysisJobRepository _jobRepository;
    private readonly IPlantRepository _plantRepository;
    private readonly IPlantDiaryRepository _diaryRepository;
    private readonly IPlantImageRepository _imageRepository;
    private readonly IPlantSourceRepository _sourceRepository;
    private readonly IPlantCareRecordRepository _careRepository;
    private readonly IPlantProfileRepository _profileRepository;
    private readonly ILogger<PlantAnalysisService> _logger;

    public PlantAnalysisService(
        IPlantAnalysisRepository analysisRepository,
        IPlantAnalysisJobRepository jobRepository,
        IPlantRepository plantRepository,
        IPlantDiaryRepository diaryRepository,
        IPlantImageRepository imageRepository,
        IPlantSourceRepository sourceRepository,
        IPlantCareRecordRepository careRepository,
        IPlantProfileRepository profileRepository,
        ILogger<PlantAnalysisService> logger)
    {
        _analysisRepository = analysisRepository;
        _jobRepository = jobRepository;
        _plantRepository = plantRepository;
        _diaryRepository = diaryRepository;
        _imageRepository = imageRepository;
        _sourceRepository = sourceRepository;
        _careRepository = careRepository;
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public async Task<List<PlantAnalysisDto>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default)
    {
        var analyses = await _analysisRepository.GetByPlantIdAsync(plantId, cancellationToken);
        return analyses.Select(a => a.ToDto()).ToList();
    }

    public async Task<PlantAnalysisJobDto> StartAnalysisAsync(int plantId, int? diaryId, AnalysisScope scope, CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var job = new PlantAnalysisJob
        {
            PlantId = plant.Id,
            DiaryId = diaryId,
            Status = AnalysisJobStatus.Pending,
            AnalysisScope = scope,
            CreatedAt = DateTime.UtcNow
        };

        await _jobRepository.AddAsync(job, cancellationToken);
        await _jobRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created analysis job {JobId} for plant {PlantId}", job.Id, plantId);
        return job.ToDto();
    }

    public async Task<PlantAnalysisJobDto> StartPhotoAnalysisAsync(int plantId, int imageId, CancellationToken cancellationToken = default)
    {
        _ = await _plantRepository.GetByIdAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var job = new PlantAnalysisJob
        {
            PlantId = plantId,
            ImageId = imageId,
            Status = AnalysisJobStatus.Pending,
            AnalysisScope = AnalysisScope.PhotoSnapshot,
            CreatedAt = DateTime.UtcNow
        };

        await _jobRepository.AddAsync(job, cancellationToken);
        await _jobRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created photo analysis job {JobId} for plant {PlantId} image {ImageId}", job.Id, plantId, imageId);
        return job.ToDto();
    }

    public async Task<PlantAnalysisJobDto?> GetJobStatusAsync(int jobId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job?.ToDto();
    }

    public async Task ProcessJobAsync(int jobId, IAIAgentService aiAgentService, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException("找不到分析工作。");

        if (job.Status != AnalysisJobStatus.Pending)
        {
            return;
        }

        job.Status = AnalysisJobStatus.Processing;
        job.StartedAt = DateTime.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);
        await _jobRepository.SaveChangesAsync(cancellationToken);

        try
        {
            var context = await BuildContextAsync(job, cancellationToken);
            var result = await aiAgentService.AnalyzePlantAsync(context, cancellationToken);
            var resultJson = JsonHelper.Serialize(result);

            var analysis = new Midnight.EC.Plant.WEB.Models.Entities.PlantAnalysis
            {
                PlantId = job.PlantId,
                DiaryId = job.DiaryId,
                ImageId = job.ImageId,
                AnalysisType = job.ImageId.HasValue ? AnalysisType.General : AnalysisType.General,
                AnalysisScope = job.AnalysisScope,
                ModelName = "OpenAI",
                PromptVersion = OpenAIPlantAgentService.PromptVersion,
                InputSnapshot = JsonHelper.Serialize(context),
                ResultJson = resultJson,
                Summary = result.Summary,
                HealthScore = result.HealthScore,
                Confidence = result.Confidence,
                CreatedAt = DateTime.UtcNow
            };

            await _analysisRepository.AddAsync(analysis, cancellationToken);
            await _analysisRepository.SaveChangesAsync(cancellationToken);

            job.AnalysisId = analysis.Id;
            job.Status = AnalysisJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(job, cancellationToken);
            await _jobRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            job.Status = AnalysisJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            job.RetryCount += 1;
            await _jobRepository.UpdateAsync(job, cancellationToken);
            await _jobRepository.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Analysis job {JobId} failed", jobId);
        }
    }

    private async Task<PlantAnalysisContext> BuildContextAsync(PlantAnalysisJob job, CancellationToken cancellationToken)
    {
        var plant = await _plantRepository.GetByIdWithDetailsAsync(job.PlantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var since = job.AnalysisScope switch
        {
            AnalysisScope.SingleDiary => DateTime.UtcNow.AddYears(-10),
            AnalysisScope.PhotoSnapshot => DateTime.UtcNow.AddYears(-10),
            AnalysisScope.Recent7Days => DateTime.UtcNow.AddDays(-7),
            AnalysisScope.Recent30Days => DateTime.UtcNow.AddDays(-30),
            _ => DateTime.UtcNow.AddYears(-10)
        };

        PlantImage? focusImage = null;
        if (job.ImageId.HasValue)
        {
            focusImage = await _imageRepository.GetByIdAsync(job.ImageId.Value, cancellationToken);
        }

        var diaries = job.AnalysisScope == AnalysisScope.PhotoSnapshot
            ? []
            : await _diaryRepository.GetRecentByPlantIdAsync(job.PlantId, since, cancellationToken);
        if (job.DiaryId.HasValue)
        {
            diaries = diaries.Where(d => d.Id == job.DiaryId.Value).ToList();
        }

        var plantImages = await _imageRepository.GetByPlantIdAsync(job.PlantId, cancellationToken);
        var images = focusImage != null ? [focusImage] : plantImages;
        var previousAnalyses = await _analysisRepository.GetByPlantIdAsync(job.PlantId, cancellationToken);
        var sources = await _sourceRepository.GetBySpeciesIdAsync(plant.SpeciesId, cancellationToken);
        var careRecords = await _careRepository.GetRecentByPlantIdAsync(job.PlantId, since, cancellationToken);
        var profile = await _profileRepository.GetByPlantIdAsync(job.PlantId, cancellationToken);

        return new PlantAnalysisContext
        {
            Plant = plant.ToDto(),
            Knowledge = plant.Species.Knowledge?.ToDto(),
            Diaries = diaries.Select(d => d.ToDto()).ToList(),
            Images = images.Select(i => i.ToDto()).ToList(),
            PreviousAnalyses = previousAnalyses.Select(a => a.ToDto()).ToList(),
            Sources = sources
                .SelectMany(s => s.Contents
                    .Where(c => c.Status == SourceContentStatus.Completed)
                    .Select(c => c.ToDto(s)))
                .ToList(),
            CareRecords = careRecords.Select(r => r.ToDto()).ToList(),
            Profile = profile?.ToDto(),
            Scope = job.AnalysisScope,
            FocusImageId = focusImage?.Id,
            FocusImageNote = focusImage?.Note,
            FocusImageAbsolutePath = focusImage == null
                ? null
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", focusImage.StoragePath),
            FocusImageContentType = focusImage?.ContentType ?? "image/jpeg"
        };
    }
}

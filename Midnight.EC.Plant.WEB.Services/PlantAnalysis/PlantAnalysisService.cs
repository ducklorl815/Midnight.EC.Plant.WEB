using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.AI;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Services.AI;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Utility.Json;

namespace Midnight.EC.Plant.WEB.Services.PlantAnalysis;

public class PlantAnalysisService
{
    private readonly PlantAnalysisRespo _analysisRepository;
    private readonly PlantAnalysisJobRespo _jobRepository;
    private readonly PlantRespo _plantRepository;
    private readonly PlantDiaryRespo _diaryRepository;
    private readonly PlantImageRespo _imageRepository;
    private readonly PlantSourceRespo _sourceRepository;
    private readonly PlantCareRecordRespo _careRepository;
    private readonly PlantProfileRespo _profileRepository;
    private readonly ILogger<PlantAnalysisService> _logger;

    public PlantAnalysisService(
        PlantAnalysisRespo analysisRepository,
        PlantAnalysisJobRespo jobRepository,
        PlantRespo plantRepository,
        PlantDiaryRespo diaryRepository,
        PlantImageRespo imageRepository,
        PlantSourceRespo sourceRepository,
        PlantCareRecordRespo careRepository,
        PlantProfileRespo profileRepository,
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

    public async Task<List<PlantAnalysisDto>> GetByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var analyses = await _analysisRepository.GetByPlantIdAsync(plantId, cancellationToken);
        return analyses.Select(a => a.ToDto()).ToList();
    }

    public async Task<PlantAnalysisJobDto> StartAnalysisAsync(Guid plantId, Guid? diaryId, AnalysisScope scope, CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var job = new PlantAnalysisJobModel
        {
            PlantId = plant.Id,
            DiaryId = diaryId,
            Status = AnalysisJobStatus.Pending,
            AnalysisScope = scope,
            CreatedAt = DateTime.UtcNow
        };

        await _jobRepository.InsertAsync(job, cancellationToken);
        _logger.LogInformation("Created analysis job {JobId} for plant {PlantId}", job.Id, plantId);
        return job.ToDto();
    }

    public async Task<PlantAnalysisJobDto> StartPhotoAnalysisAsync(Guid plantId, Guid imageId, CancellationToken cancellationToken = default)
    {
        _ = await _plantRepository.GetByIdAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var job = new PlantAnalysisJobModel
        {
            PlantId = plantId,
            ImageId = imageId,
            Status = AnalysisJobStatus.Pending,
            AnalysisScope = AnalysisScope.PhotoSnapshot,
            CreatedAt = DateTime.UtcNow
        };

        await _jobRepository.InsertAsync(job, cancellationToken);
        _logger.LogInformation("Created photo analysis job {JobId} for plant {PlantId} image {ImageId}", job.Id, plantId, imageId);
        return job.ToDto();
    }

    public async Task<PlantAnalysisJobDto?> GetJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job?.ToDto();
    }

    public async Task ProcessJobAsync(Guid jobId, IAIAgentService aiAgentService, CancellationToken cancellationToken = default)
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
        try
        {
            var context = await BuildContextAsync(job, cancellationToken);
            var result = await aiAgentService.AnalyzePlantAsync(context, cancellationToken);
            var resultJson = JsonHelper.Serialize(result);

            var analysis = new Midnight.EC.Plant.WEB.Models.Models.PlantAnalysisModel
            {
                PlantId = job.PlantID,
                DiaryId = job.DiaryID,
                ImageId = job.ImageID,
                AnalysisType = job.ImageID.HasValue ? AnalysisType.General : AnalysisType.General,
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

            await _analysisRepository.InsertAsync(analysis, cancellationToken);
            job.AnalysisID = analysis.Id;
            job.Status = AnalysisJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(job, cancellationToken);
        }
        catch (Exception ex)
        {
            job.Status = AnalysisJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            job.RetryCount += 1;
            await _jobRepository.UpdateAsync(job, cancellationToken);
            _logger.LogError(ex, "Analysis job {JobId} failed", jobId);
        }
    }

    private async Task<PlantAnalysisContext> BuildContextAsync(PlantAnalysisJobModel job, CancellationToken cancellationToken)
    {
        var plant = await _plantRepository.GetByIdWithDetailsAsync(job.PlantID, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var since = job.AnalysisScope switch
        {
            AnalysisScope.SingleDiary => DateTime.UtcNow.AddYears(-10),
            AnalysisScope.PhotoSnapshot => DateTime.UtcNow.AddYears(-10),
            AnalysisScope.Recent7Days => DateTime.UtcNow.AddDays(-7),
            AnalysisScope.Recent30Days => DateTime.UtcNow.AddDays(-30),
            _ => DateTime.UtcNow.AddYears(-10)
        };

        PlantImageModel? focusImage = null;
        if (job.ImageID.HasValue)
        {
            focusImage = await _imageRepository.GetByIdAsync(job.ImageID.Value, cancellationToken);
        }

        var diaries = job.AnalysisScope == AnalysisScope.PhotoSnapshot
            ? []
            : await _diaryRepository.GetRecentByPlantIdAsync(job.PlantID, since, cancellationToken);
        if (job.DiaryID.HasValue)
        {
            diaries = diaries.Where(d => d.Id == job.DiaryID.Value).ToList();
        }

        var plantImages = await _imageRepository.GetByPlantIdAsync(job.PlantID, cancellationToken);
        var images = focusImage != null ? [focusImage] : plantImages;
        var previousAnalyses = await _analysisRepository.GetByPlantIdAsync(job.PlantID, cancellationToken);
        var sources = await _sourceRepository.GetBySpeciesIdAsync(plant.SpeciesID, cancellationToken);
        var careRecords = await _careRepository.GetRecentByPlantIdAsync(job.PlantID, since, cancellationToken);
        var profile = await _profileRepository.GetByPlantIdAsync(job.PlantID, cancellationToken);

        return new PlantAnalysisContext
        {
            Plant = plant.ToDto(),
            Knowledge = plant.Species.Knowledge?.ToDto(),
            Diaries = diaries.Select(d => d.ToDto()).ToList(),
            Images = images.Select(i => i.ToDto()).ToList(),
            PreviousAnalyses = previousAnalyses.Select(a => a.ToDto()).ToList(),
            Sources = (await Task.WhenAll(sources.Select(async s =>
                {
                    var contents = await _sourceRepository.GetContentsBySourceIdAsync(s.Id, cancellationToken);
                    return contents
                        .Where(c => c.Status == SourceContentStatus.Completed)
                        .Select(c => c.ToDto(s));
                })))
                .SelectMany(x => x)
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

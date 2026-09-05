using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Models.AI;

namespace Midnight.EC.Plant.WEB.Services.Interfaces;

public class CreatePlantDraft
{
    public string ChineseName { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public bool WateredToday { get; set; } = true;

    public PlacementType? ActualPlacement { get; set; }
    public LightLevel? ActualLight { get; set; }
    public bool? HasRainCover { get; set; }
    public string? SubstrateType { get; set; }
    public SaucerState? SaucerState { get; set; }
    public string? City { get; set; }
    public bool EnvironmentMismatchAcknowledged { get; set; }
}

public interface IExternalPlantApiService
{
    Task<ExternalPlantSearchResult?> SearchSpeciesAsync(string keyword, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExternalSpeciesResult>> SearchSpeciesCandidatesAsync(string keyword, CancellationToken cancellationToken = default);
    Task<PlantIdentificationResult?> IdentifyFromImageAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default);
}

public interface IImageStorageService
{
    Task<StoredImageResult> SaveAsync(Stream stream, string originalFileName, string contentType, Guid plantId, CancellationToken cancellationToken = default);
    string GetPublicPath(string storagePath);
}

public class StoredImageResult
{
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public long FileSize { get; set; }
    public string? Sha256 { get; set; }
}

public interface IAIAgentService
{
    Task<PlantAnalysisResultDto> AnalyzePlantAsync(PlantAnalysisContext context, CancellationToken cancellationToken = default);
}

public interface ICareKnowledgeSynthesisService
{
    Task<CareSynthesisResult> SynthesizeAsync(
        string speciesKeyword,
        string? scientificName,
        ExternalKnowledgeResult mergedKnowledge,
        CancellationToken cancellationToken = default,
        bool forceRefreshGuide = false);

    Task<EnvironmentFitResult> SynthesizeEnvironmentFitAsync(
        string plantDisplayName,
        string? scientificName,
        ExternalKnowledgeResult knowledge,
        PlantEnvironmentContext environment,
        CancellationToken cancellationToken = default);
}

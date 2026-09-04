using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.External;

namespace Midnight.EC.Plant.WEB.Services.Interfaces;

public interface IPlantService
{
    Task<List<PlantDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PlantDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PlantDto> CreateAsync(string name, string? speciesKeyword, string? nickName, string? location, string? description, DateTime? startDate, CancellationToken cancellationToken = default);
    Task<PlantDto> CreateAsync(string name, string? speciesKeyword, Stream? identificationImage, string? identificationFileName, string? nickName, string? location, string? description, DateTime? startDate, CancellationToken cancellationToken = default);
    Task<bool> IsNickNameTakenAsync(string nickName, int? excludePlantId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExternalSpeciesResult>> SearchSpeciesCandidatesAsync(string keyword, CancellationToken cancellationToken = default);
    Task<PlantDto> CreateFromDraftAsync(CreatePlantDraft draft, ExternalSpeciesResult? confirmedSpecies, CancellationToken cancellationToken = default);
    Task<PlantDto> UpdateAsync(int id, string name, string? nickName, string? location, string? description, DateTime? startDate, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class CreatePlantDraft
{
    public string ChineseName { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public bool WateredToday { get; set; } = true;
}

public interface IPlantKnowledgeService
{
    Task<PlantKnowledgeDto?> GetBySpeciesIdAsync(int speciesId, CancellationToken cancellationToken = default);
    Task<PlantKnowledgeDto> SyncFromExternalAsync(int speciesId, string speciesKeyword, CancellationToken cancellationToken = default);
    Task<PlantKnowledgeDto> RefreshFromExternalAsync(int speciesId, string speciesKeyword, CancellationToken cancellationToken = default);
    Task<PlantKnowledgeDto> RefreshFromExternalAsync(int speciesId, string? speciesKeyword, Stream? identificationImage, string? imageFileName, CancellationToken cancellationToken = default);
}

public interface IPlantDiaryService
{
    Task<List<PlantDiaryDto>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task<PlantDiaryDto> CreateAsync(int plantId, DateTime diaryDate, string? title, string? note, CancellationToken cancellationToken = default);
    Task DeleteAsync(int diaryId, CancellationToken cancellationToken = default);
}

public interface IPlantAnalysisService
{
    Task<List<PlantAnalysisDto>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task<PlantAnalysisJobDto> StartAnalysisAsync(int plantId, int? diaryId, AnalysisScope scope, CancellationToken cancellationToken = default);
    Task<PlantAnalysisJobDto> StartPhotoAnalysisAsync(int plantId, int imageId, CancellationToken cancellationToken = default);
    Task<PlantAnalysisJobDto?> GetJobStatusAsync(int jobId, CancellationToken cancellationToken = default);
}

public interface IExternalPlantApiService
{
    Task<ExternalPlantSearchResult?> SearchSpeciesAsync(string keyword, CancellationToken cancellationToken = default);
    /// <summary>iNaturalist + GBIF 為主，學名正規化去重後最多回傳 3 筆。</summary>
    Task<IReadOnlyList<ExternalSpeciesResult>> SearchSpeciesCandidatesAsync(string keyword, CancellationToken cancellationToken = default);
    Task<PlantIdentificationResult?> IdentifyFromImageAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default);
}

public interface IImageStorageService
{
    Task<StoredImageResult> SaveAsync(Stream stream, string originalFileName, string contentType, int plantId, CancellationToken cancellationToken = default);
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
    Task<Midnight.EC.Plant.WEB.Models.AI.PlantAnalysisResultDto> AnalyzePlantAsync(Midnight.EC.Plant.WEB.Models.AI.PlantAnalysisContext context, CancellationToken cancellationToken = default);
}

public interface ICareKnowledgeSynthesisService
{
    Task<ExternalKnowledgePartial?> SynthesizeAsync(
        string speciesKeyword,
        string? scientificName,
        ExternalKnowledgeResult mergedKnowledge,
        CancellationToken cancellationToken = default);
}

public interface IPlantImageService
{
    Task<List<PlantImageDto>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task<PlantImageDto?> GetCoverAsync(int plantId, CancellationToken cancellationToken = default);
    Task<PlantImageDto> UploadAsync(int plantId, Stream stream, string fileName, string contentType, string? note, bool setAsCover, CancellationToken cancellationToken = default);
    Task SetCoverAsync(int plantId, int imageId, CancellationToken cancellationToken = default);
    Task UpdateNoteAsync(int imageId, string? note, CancellationToken cancellationToken = default);
    Task DeleteAsync(int imageId, CancellationToken cancellationToken = default);
}

public interface IPlantSourceService
{
    Task<List<PlantSourceListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PlantSourceDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ParsedContentDto> ParseUrlAsync(string url, SourceType? sourceType = null, CancellationToken cancellationToken = default);
    Task<PlantSourceDetailDto> SaveAsync(IEnumerable<int> speciesIds, ParsedContentDto parsed, string? titleOverride, CancellationToken cancellationToken = default);
    Task<PlantSourceDetailDto> ReparseAsync(int sourceId, CancellationToken cancellationToken = default);
}

public interface IPlantCareService
{
    Task<List<PlantCareRecordDto>> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task<PlantCareRecordDto> CreateAsync(int plantId, DateTime recordDate, CareRecordType careType, decimal? numericValue, string? unit, string? note, CancellationToken cancellationToken = default);
    Task<PlantTrendDto> GetTrendAsync(int plantId, int days = 30, CancellationToken cancellationToken = default);
}

public interface IPlantProfileService
{
    Task<PlantProfileDto?> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task<PlantProfileDto> SaveAsync(int plantId, PlantProfileDto model, CancellationToken cancellationToken = default);
}

public interface IPlantReminderService
{
    Task<List<PlantReminderDto>> GetActiveByPlantIdAsync(int plantId, CancellationToken cancellationToken = default);
    Task<List<PlantDashboardItemDto>> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task SyncRemindersAsync(int? plantId, CancellationToken cancellationToken = default);
    Task DismissAsync(int reminderId, CancellationToken cancellationToken = default);
}

public interface IPlantTimelineService
{
    Task<List<PlantTimelineEventDto>> GetTimelineAsync(int plantId, int days = 90, CancellationToken cancellationToken = default);
}

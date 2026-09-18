using Midnight.EC.Plant.WEB.Models.Enums;

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

public interface IImageStorageService
{
    Task<StoredImageResult> SaveAsync(Stream stream, string originalFileName, string contentType, Guid plantId, CancellationToken cancellationToken = default);
    /// <summary>站點級媒體（如拼圖 Banner），不屬於任何一盆。</summary>
    Task<StoredImageResult> SaveSiteMediaAsync(Stream stream, string originalFileName, string contentType, string relativeSubfolder, CancellationToken cancellationToken = default);
    Task<StoredImageResult> SaveEffectAsync(Stream stream, string fileName, string contentType, Guid plantId, CancellationToken cancellationToken = default);
    string GetPublicPath(string storagePath);
    string GetAbsolutePath(string storagePath);
}

public class StoredImageResult
{
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public long FileSize { get; set; }
    public string? Sha256 { get; set; }
}

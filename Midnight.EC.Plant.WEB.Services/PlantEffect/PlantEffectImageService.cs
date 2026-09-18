using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Services.PageComposer;

namespace Midnight.EC.Plant.WEB.Services.PlantEffect;

public class PlantEffectImageService
{
    private readonly PlantRespo _plantRespo;
    private readonly PlantImageRespo _imageRespo;
    private readonly PlantEffectImageRespo _effectRespo;
    private readonly IOpenAIImageService _openAIImages;
    private readonly IImageStorageService _storage;
    private readonly ILogger<PlantEffectImageService> _logger;

    public PlantEffectImageService(
        PlantRespo plantRespo,
        PlantImageRespo imageRespo,
        PlantEffectImageRespo effectRespo,
        IOpenAIImageService openAIImages,
        IImageStorageService storage,
        ILogger<PlantEffectImageService> logger)
    {
        _plantRespo = plantRespo;
        _imageRespo = imageRespo;
        _effectRespo = effectRespo;
        _openAIImages = openAIImages;
        _storage = storage;
        _logger = logger;
    }

    public static string? ResolveLeftImage(string? coverImagePath, string? swappedEffectImageUrl)
    {
        if (!string.IsNullOrWhiteSpace(swappedEffectImageUrl))
            return swappedEffectImageUrl.Trim();
        return string.IsNullOrWhiteSpace(coverImagePath) ? null : coverImagePath;
    }

    public async Task<PlantEffectImageDto?> GetLatestAsync(Guid photoId, CancellationToken cancellationToken = default)
    {
        var row = await _effectRespo.GetLatestByOriginalPhotoIdAsync(photoId, cancellationToken);
        if (row == null) return null;
        var plant = await _plantRespo.GetByIdWithDetailsAsync(row.PlantID, cancellationToken)
            ?? await _plantRespo.GetByIdAsync(row.PlantID, cancellationToken);
        return MapDto(row, plant);
    }

    public async Task<IReadOnlyDictionary<Guid, PlantEffectImageDto>> GetLatestByPhotoIdsAsync(
        IReadOnlyCollection<Guid> photoIds,
        CancellationToken cancellationToken = default)
    {
        var rows = await _effectRespo.GetLatestByOriginalPhotoIdsAsync(photoIds, cancellationToken);
        return rows.ToDictionary(
            kv => kv.Key,
            kv =>
            {
                var dto = kv.Value.ToDto();
                dto.GeneratedImageUrl = _storage.GetPublicPath(kv.Value.GeneratedImagePath);
                return dto;
            });
    }

    public async Task<PlantEffectImageDto> GenerateAsync(
        Guid photoId,
        bool forceRegenerate,
        CancellationToken cancellationToken = default)
    {
        var photo = await _imageRespo.GetByIdAsync(photoId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物照片。");

        if (!forceRegenerate)
        {
            var existing = await _effectRespo.GetLatestByOriginalPhotoIdAsync(photoId, cancellationToken);
            if (existing != null && existing.Status == EffectImageStatus.Completed)
            {
                var plantExisting = await _plantRespo.GetByIdWithDetailsAsync(existing.PlantID, cancellationToken)
                    ?? await _plantRespo.GetByIdAsync(existing.PlantID, cancellationToken);
                return MapDto(existing, plantExisting);
            }
        }

        var plant = await _plantRespo.GetByIdWithDetailsAsync(photo.PlantID, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var overlay = BuildOverlay(plant);
        var composition = PlantVisualCompositionService.CreateGoldenReferencePreset(
            overlay.DisplayName,
            overlay.ScientificName,
            plant.Species?.Knowledge?.CareSummary ?? plant.Description);
        var prompt = PlantEffectPromptBuilder.Build(plant, composition, overlay);

        var absolutePhoto = _storage.GetAbsolutePath(photo.StoragePath);

        var pending = new PlantEffectImageModel
        {
            PlantID = plant.ID,
            OriginalPhotoID = photo.ID,
            GeneratedImagePath = "",
            Style = composition.Style,
            Layout = composition.Layout,
            ColorPalette = composition.ColorPalette,
            DecorationJson = composition.ToDecorationJson(),
            PromptVersion = PlantEffectPromptBuilder.PromptVersion,
            PromptText = Truncate(prompt, 8000),
            Status = EffectImageStatus.Processing,
            IsLatest = false
        };

        try
        {
            var baseResult = await _openAIImages.EditFromFileAsync(absolutePhoto, prompt, cancellationToken);

            await using var ms = new MemoryStream(baseResult.ImageBytes);
            var stored = await _storage.SaveEffectAsync(
                ms,
                $"effect-{photo.ID:N}.png",
                baseResult.ContentType,
                plant.ID,
                cancellationToken);

            await _effectRespo.ClearLatestAsync(photo.ID, cancellationToken);

            pending.GeneratedImagePath = stored.StoragePath;
            pending.Status = EffectImageStatus.Completed;
            pending.GenerationRequestId = baseResult.RequestId;
            pending.IsLatest = true;
            pending.ErrorMessage = null;
            await _effectRespo.InsertAsync(pending, cancellationToken);

            _logger.LogInformation(
                "Generated effect image {EffectId} for plant {PlantId} photo {PhotoId} requestId={RequestId} model={Model}",
                pending.ID,
                plant.ID,
                photo.ID,
                baseResult.RequestId,
                baseResult.Model);

            return MapDto(pending, plant, overlay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Effect image generation failed for photo {PhotoId}", photoId);
            pending.Status = EffectImageStatus.Failed;
            pending.ErrorMessage = Truncate(ex.Message, 900);
            pending.GeneratedImagePath = "(failed)";
            pending.IsLatest = false;
            try
            {
                await _effectRespo.InsertAsync(pending, cancellationToken);
            }
            catch (Exception insertEx)
            {
                _logger.LogWarning(insertEx, "Failed to persist failed effect record.");
            }

            throw;
        }
    }

    private PlantEffectImageDto MapDto(
        PlantEffectImageModel row,
        PlantModel? plant,
        PlantEffectOverlayDto? overlay = null)
    {
        var dto = row.ToDto();
        dto.GeneratedImageUrl = string.IsNullOrWhiteSpace(row.GeneratedImagePath) || row.GeneratedImagePath == "(failed)"
            ? null
            : _storage.GetPublicPath(row.GeneratedImagePath);
        dto.Overlay = overlay ?? (plant == null ? null : BuildOverlay(plant));
        return dto;
    }

    public static PlantEffectOverlayDto BuildOverlay(PlantModel plant)
    {
        var seq = plant.Seq > 0 ? plant.Seq : Math.Abs(plant.ID.GetHashCode() % 100);
        return new PlantEffectOverlayDto
        {
            DisplayName = PlantDetailWallMapper.ResolveDisplayName(plant),
            ScientificName = string.IsNullOrWhiteSpace(plant.Species?.ScientificName)
                ? null
                : plant.Species!.ScientificName.Trim(),
            Intro = PlantDetailWallMapper.ResolveIntro(plant),
            SpecimenLabel = $"SPECIMEN {seq % 100:D2}",
            CareFacts = PlantDetailWallMapper.BuildCareFacts(plant.Species?.Knowledge)
        };
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= max ? value : value[..max] + "…";
    }
}

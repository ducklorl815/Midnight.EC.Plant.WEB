using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Services.Configuration;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Utility.Hash;

namespace Midnight.EC.Plant.WEB.Services.PlantDiary;

public class LocalImageStorageService : IImageStorageService
{
    private readonly StorageOptions _options;

    public LocalImageStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<StoredImageResult> SaveAsync(Stream stream, string originalFileName, string contentType, Guid plantId, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativeFolder = Path.Combine(_options.RootPath, plantId.ToString(), "photos");
        var absoluteFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var absolutePath = Path.Combine(absoluteFolder, fileName);
        await using (var fileStream = File.Create(absolutePath))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        var relativePath = Path.Combine(relativeFolder, fileName).Replace('\\', '/');
        var sha256 = await ComputeSha256Async(absolutePath, cancellationToken);

        return new StoredImageResult
        {
            FileName = fileName,
            StoragePath = relativePath,
            ThumbnailPath = relativePath,
            FileSize = new FileInfo(absolutePath).Length,
            Sha256 = sha256
        };
    }

    public async Task<StoredImageResult> SaveSiteMediaAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        string relativeSubfolder,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = GuessExtension(contentType);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var safeFolder = (relativeSubfolder ?? "page-composer")
            .Replace('\\', '/')
            .Trim('/')
            .Replace("..", "");
        var relativeFolder = Path.Combine("uploads", safeFolder);
        var absoluteFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var absolutePath = Path.Combine(absoluteFolder, fileName);
        await using (var fileStream = File.Create(absolutePath))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        var relativePath = Path.Combine(relativeFolder, fileName).Replace('\\', '/');
        var sha256 = await ComputeSha256Async(absolutePath, cancellationToken);

        return new StoredImageResult
        {
            FileName = fileName,
            StoragePath = relativePath,
            ThumbnailPath = relativePath,
            FileSize = new FileInfo(absolutePath).Length,
            Sha256 = sha256
        };
    }

    private static string GuessExtension(string contentType) =>
        contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg"
        };

    public string GetPublicPath(string storagePath) => $"/{storagePath.Replace('\\', '/')}";

    public string GetAbsolutePath(string storagePath) =>
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", storagePath.Replace('/', Path.DirectorySeparatorChar));

    public async Task<StoredImageResult> SaveEffectAsync(
        Stream stream,
        string fileName,
        string contentType,
        Guid plantId,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = contentType?.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".png"
            };

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var relativeFolder = Path.Combine(_options.RootPath, plantId.ToString(), "effects");
        var absoluteFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var absolutePath = Path.Combine(absoluteFolder, storedName);
        await using (var fileStream = File.Create(absolutePath))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        var relativePath = Path.Combine(relativeFolder, storedName).Replace('\\', '/');
        return new StoredImageResult
        {
            FileName = storedName,
            StoragePath = relativePath,
            ThumbnailPath = relativePath,
            FileSize = new FileInfo(absolutePath).Length,
            Sha256 = null
        };
    }

    private static async Task<string> ComputeSha256Async(string absolutePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(absolutePath);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return HashHelper.ComputeSha256(Convert.ToBase64String(memory.ToArray()));
    }
}

public class PlantImageService
{
    private readonly PlantRespo _plantRepository;
    private readonly PlantImageRespo _imageRepository;
    private readonly IImageStorageService _imageStorageService;

    public PlantImageService(
        PlantRespo plantRepository,
        PlantImageRespo imageRepository,
        IImageStorageService imageStorageService)
    {
        _plantRepository = plantRepository;
        _imageRepository = imageRepository;
        _imageStorageService = imageStorageService;
    }

    public async Task<List<PlantImageDto>> GetByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var images = await _imageRepository.GetByPlantIdAsync(plantId, cancellationToken);
        return images.Select(i => i.ToDto()).ToList();
    }

    public async Task<PlantImageDto?> GetCoverAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var cover = await _imageRepository.GetCoverByPlantIdAsync(plantId, cancellationToken);
        return cover?.ToDto();
    }

    public async Task<PlantImageDto> UploadAsync(
        Guid plantId,
        Stream stream,
        string fileName,
        string contentType,
        string? note,
        bool setAsCover,
        CancellationToken cancellationToken = default)
    {
        _ = await _plantRepository.GetByIdAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var stored = await _imageStorageService.SaveAsync(stream, fileName, contentType, plantId, cancellationToken);

        if (setAsCover)
        {
            await _imageRepository.ClearCoverAsync(plantId, cancellationToken);
        }

        var image = new PlantImageModel
        {
            PlantId = plantId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            IsCover = setAsCover,
            FileName = stored.FileName,
            StoragePath = stored.StoragePath,
            ThumbnailPath = stored.ThumbnailPath,
            OriginalFileName = fileName,
            ContentType = contentType,
            FileSize = stored.FileSize,
            Sha256 = stored.Sha256,
            CreatedAt = DateTime.UtcNow
        };

        await _imageRepository.InsertAsync(image, cancellationToken);
        return image.ToDto();
    }

    public async Task SetCoverAsync(Guid plantId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await _imageRepository.GetByIdAsync(imageId, cancellationToken)
            ?? throw new InvalidOperationException("找不到圖片。");

        if (image.PlantID != plantId)
        {
            throw new InvalidOperationException("圖片不屬於指定植物。");
        }

        await _imageRepository.ClearCoverAsync(plantId, cancellationToken);
        image.IsCover = true;
        await _imageRepository.UpdateAsync(image, cancellationToken);
    }

    public async Task DeleteAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await _imageRepository.GetByIdAsync(imageId, cancellationToken)
            ?? throw new InvalidOperationException("找不到圖片。");

        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.StoragePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        await _imageRepository.SoftDeleteAsync(image.Id, cancellationToken);
    }

    public async Task UpdateNoteAsync(Guid imageId, string? note, CancellationToken cancellationToken = default)
    {
        var image = await _imageRepository.GetByIdAsync(imageId, cancellationToken)
            ?? throw new InvalidOperationException("找不到圖片。");

        image.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        await _imageRepository.UpdateAsync(image, cancellationToken);
    }
}

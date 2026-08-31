using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.ContentParser;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Utility.Hash;
using Midnight.EC.Plant.WEB.Utility.Json;
using Midnight.EC.Plant.WEB.Utility.Url;

namespace Midnight.EC.Plant.WEB.Services.PlantSource;

public class PlantSourceService : IPlantSourceService
{
    private readonly IPlantSourceRepository _sourceRepository;
    private readonly IPlantSpeciesRepository _speciesRepository;
    private readonly IContentFetcher _contentFetcher;
    private readonly IPlantContentParserFactory _parserFactory;
    private readonly ILogger<PlantSourceService> _logger;

    public PlantSourceService(
        IPlantSourceRepository sourceRepository,
        IPlantSpeciesRepository speciesRepository,
        IContentFetcher contentFetcher,
        IPlantContentParserFactory parserFactory,
        ILogger<PlantSourceService> logger)
    {
        _sourceRepository = sourceRepository;
        _speciesRepository = speciesRepository;
        _contentFetcher = contentFetcher;
        _parserFactory = parserFactory;
        _logger = logger;
    }

    public async Task<List<PlantSourceListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sources = await _sourceRepository.GetAllAsync(cancellationToken);
        return sources.Select(MapListItem).ToList();
    }

    public async Task<PlantSourceDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var source = await _sourceRepository.GetByIdWithContentsAsync(id, cancellationToken);
        return source == null ? null : MapDetail(source);
    }

    public async Task<ParsedContentDto> ParseUrlAsync(string url, SourceType? sourceType = null, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = UrlNormalizer.Normalize(url);
        if (string.IsNullOrWhiteSpace(normalizedUrl) || !Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("URL 格式不正確。");
        }

        var urlHash = HashHelper.ComputeSha256(normalizedUrl);
        var existing = await _sourceRepository.GetByUrlHashAsync(urlHash, cancellationToken);
        if (existing != null)
        {
            var latest = existing.Contents.OrderByDescending(c => c.ParsedAt ?? c.UpdatedAt).FirstOrDefault();
            return new ParsedContentDto
            {
                NormalizedUrl = normalizedUrl,
                UrlHash = urlHash,
                SourceType = existing.SourceType,
                Title = existing.Title,
                Author = existing.Author,
                Domain = existing.Domain,
                CleanText = latest?.CleanText,
                Summary = latest?.Summary,
                Keywords = latest?.Keywords,
                ContentHash = latest?.ContentHash,
                ParserType = latest?.ParserType ?? "Cached",
                ParserVersion = latest?.ParserVersion ?? "1.0",
                ReliabilityLevel = existing.ReliabilityLevel,
                Status = latest?.Status ?? SourceContentStatus.Completed,
                ErrorMessage = latest?.ErrorMessage,
                IsExisting = true,
                ExistingSourceId = existing.Id
            };
        }

        var detectedType = sourceType ?? SourceTypeDetector.Detect(normalizedUrl);
        var html = await _contentFetcher.FetchAsync(normalizedUrl, cancellationToken);
        var parser = _parserFactory.Resolve(uri, detectedType);
        var parsed = await parser.ParseAsync(uri, html, cancellationToken);
        parsed.NormalizedUrl = normalizedUrl;
        parsed.UrlHash = urlHash;
        parsed.SourceType = detectedType;
        parsed.ReliabilityLevel = SourceTypeDetector.SuggestReliabilityLevel(detectedType);

        _logger.LogInformation("Parsed URL {Url} using {ParserType}", normalizedUrl, parsed.ParserType);
        return parsed;
    }

    public async Task<PlantSourceDetailDto> SaveAsync(int speciesId, ParsedContentDto parsed, string? titleOverride, CancellationToken cancellationToken = default)
    {
        var species = await _speciesRepository.GetByIdAsync(speciesId, cancellationToken)
            ?? throw new InvalidOperationException("找不到物種。");

        var urlHash = parsed.UrlHash;
        if (string.IsNullOrWhiteSpace(urlHash))
        {
            urlHash = HashHelper.ComputeSha256(parsed.NormalizedUrl);
        }

        var existing = await _sourceRepository.GetByUrlHashAsync(urlHash, cancellationToken);
        if (existing != null)
        {
            return MapDetail(existing);
        }

        var now = DateTime.UtcNow;
        var source = new Midnight.EC.Plant.WEB.Models.Entities.PlantSource
        {
            SpeciesId = species.Id,
            SourceType = parsed.SourceType,
            Title = titleOverride ?? parsed.Title,
            Url = parsed.NormalizedUrl,
            Domain = parsed.Domain ?? new Uri(parsed.NormalizedUrl).Host,
            Author = parsed.Author,
            PublishedAt = parsed.PublishedAt,
            ContentHash = urlHash,
            ReliabilityLevel = parsed.ReliabilityLevel,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Contents =
            [
                new PlantSourceContent
                {
                    RawText = parsed.RawText,
                    CleanText = parsed.CleanText,
                    Summary = parsed.Summary,
                    Keywords = parsed.Keywords,
                    ParsedJson = JsonHelper.Serialize(parsed),
                    ParserType = parsed.ParserType,
                    ParserVersion = parsed.ParserVersion,
                    ContentHash = parsed.ContentHash,
                    Status = parsed.Status,
                    ErrorMessage = parsed.ErrorMessage,
                    ParsedAt = now,
                    UpdatedAt = now
                }
            ]
        };

        await _sourceRepository.AddAsync(source, cancellationToken);
        await _sourceRepository.SaveChangesAsync(cancellationToken);

        var created = await _sourceRepository.GetByIdWithContentsAsync(source.Id, cancellationToken)
            ?? throw new InvalidOperationException("儲存來源後無法讀取資料。");

        return MapDetail(created);
    }

    public async Task<PlantSourceDetailDto> ReparseAsync(int sourceId, CancellationToken cancellationToken = default)
    {
        var source = await _sourceRepository.GetByIdWithContentsAsync(sourceId, cancellationToken)
            ?? throw new InvalidOperationException("找不到來源。");

        var parsed = await ParseUrlAsync(source.Url, source.SourceType, cancellationToken);
        var now = DateTime.UtcNow;

        source.Title = parsed.Title ?? source.Title;
        source.Author = parsed.Author ?? source.Author;
        source.UpdatedAt = now;

        source.Contents.Add(new PlantSourceContent
        {
            RawText = parsed.RawText,
            CleanText = parsed.CleanText,
            Summary = parsed.Summary,
            Keywords = parsed.Keywords,
            ParsedJson = JsonHelper.Serialize(parsed),
            ParserType = parsed.ParserType,
            ParserVersion = parsed.ParserVersion,
            ContentHash = parsed.ContentHash,
            Status = parsed.Status,
            ErrorMessage = parsed.ErrorMessage,
            ParsedAt = now,
            UpdatedAt = now
        });

        await _sourceRepository.UpdateAsync(source, cancellationToken);
        await _sourceRepository.SaveChangesAsync(cancellationToken);

        var updated = await _sourceRepository.GetByIdWithContentsAsync(sourceId, cancellationToken)
            ?? throw new InvalidOperationException("重新解析後無法讀取資料。");

        return MapDetail(updated);
    }

    private static PlantSourceListItemDto MapListItem(Midnight.EC.Plant.WEB.Models.Entities.PlantSource source) => new()
    {
        Id = source.Id,
        SpeciesId = source.SpeciesId,
        SpeciesName = source.Species?.ChineseName ?? source.Species?.CommonName ?? source.Species?.ScientificName,
        Title = source.Title,
        Url = source.Url,
        SourceType = source.SourceType,
        ReliabilityLevel = source.ReliabilityLevel,
        IsActive = source.IsActive,
        CreatedAt = source.CreatedAt
    };

    private static PlantSourceDetailDto MapDetail(Midnight.EC.Plant.WEB.Models.Entities.PlantSource source)
    {
        var latest = source.Contents.OrderByDescending(c => c.ParsedAt ?? c.UpdatedAt).FirstOrDefault();
        return new PlantSourceDetailDto
        {
            Id = source.Id,
            SpeciesId = source.SpeciesId,
            SpeciesName = source.Species?.ChineseName ?? source.Species?.CommonName ?? source.Species?.ScientificName,
            SourceType = source.SourceType,
            Title = source.Title,
            Url = source.Url,
            Domain = source.Domain,
            Author = source.Author,
            ReliabilityLevel = source.ReliabilityLevel,
            ContentHash = source.ContentHash,
            IsActive = source.IsActive,
            CreatedAt = source.CreatedAt,
            LatestContent = latest == null ? null : new PlantSourceContentDetailDto
            {
                Id = latest.Id,
                CleanText = latest.CleanText,
                Summary = latest.Summary,
                Keywords = latest.Keywords,
                ContentHash = latest.ContentHash,
                ParserType = latest.ParserType,
                Status = latest.Status,
                ErrorMessage = latest.ErrorMessage,
                ParsedAt = latest.ParsedAt
            }
        };
    }
}

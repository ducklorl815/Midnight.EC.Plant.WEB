using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Services.ContentParser;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Utility.Hash;
using Midnight.EC.Plant.WEB.Utility.Json;
using Midnight.EC.Plant.WEB.Utility.Url;

namespace Midnight.EC.Plant.WEB.Services.PlantSource;

public class PlantSourceService
{
    private readonly PlantSourceRespo _sourceRepository;
    private readonly PlantSpeciesRespo _speciesRepository;
    private readonly IContentFetcher _contentFetcher;
    private readonly IPlantContentParserFactory _parserFactory;
    private readonly ILogger<PlantSourceService> _logger;

    public PlantSourceService(
        PlantSourceRespo sourceRepository,
        PlantSpeciesRespo speciesRepository,
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
        var result = new List<PlantSourceListItemDto>();
        foreach (var source in sources)
        {
            result.Add(await MapListItemAsync(source, cancellationToken));
        }
        return result;
    }

    public async Task<PlantSourceDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await _sourceRepository.GetByIdAsync(id, cancellationToken);
        return source == null ? null : await MapDetailAsync(source, cancellationToken);
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
            var contents = await _sourceRepository.GetContentsBySourceIdAsync(existing.Id, cancellationToken);
            var latest = contents.OrderByDescending(c => c.ParsedAt ?? c.ModifyDate).FirstOrDefault();
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

    public Task<PlantSourceDetailDto> SaveAsync(
        IEnumerable<Guid> speciesIds,
        ParsedContentDto parsed,
        string? titleOverride,
        CancellationToken cancellationToken = default) =>
        SaveInternalAsync(speciesIds, parsed, titleOverride, cancellationToken);

    public async Task<PlantSourceDetailDto> ReparseAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        var source = await _sourceRepository.GetByIdAsync(sourceId, cancellationToken)
            ?? throw new InvalidOperationException("找不到來源。");

        var parsed = await ParseUrlAsync(source.Url, source.SourceType, cancellationToken);
        var now = DateTime.UtcNow;

        source.Title = parsed.Title ?? source.Title;
        source.Author = parsed.Author ?? source.Author;
        await _sourceRepository.UpdateAsync(source, cancellationToken);

        await _sourceRepository.InsertContentAsync(new PlantSourceContentModel
        {
            SourceID = source.Id,
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
            ParsedAt = now
        }, cancellationToken);

        return (await GetByIdAsync(sourceId, cancellationToken))!;
    }

    private async Task<PlantSourceDetailDto> SaveInternalAsync(
        IEnumerable<Guid> speciesIds,
        ParsedContentDto parsed,
        string? titleOverride,
        CancellationToken cancellationToken)
    {
        var idList = speciesIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0)
        {
            throw new InvalidOperationException("請至少選擇一個物種。");
        }

        foreach (var speciesId in idList)
        {
            _ = await _speciesRepository.GetByIdAsync(speciesId, cancellationToken)
                ?? throw new InvalidOperationException($"找不到物種（ID: {speciesId}）。");
        }

        var urlHash = string.IsNullOrWhiteSpace(parsed.UrlHash)
            ? HashHelper.ComputeSha256(parsed.NormalizedUrl)
            : parsed.UrlHash;

        var existing = await _sourceRepository.GetByUrlHashAsync(urlHash, cancellationToken);
        if (existing != null)
        {
            foreach (var speciesId in idList)
            {
                await _sourceRepository.LinkSpeciesAsync(existing.Id, speciesId, cancellationToken);
            }
            return (await GetByIdAsync(existing.Id, cancellationToken))!;
        }

        var now = DateTime.UtcNow;
        var source = new PlantSourceModel
        {
            SpeciesId = idList[0],
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
            UpdatedAt = now
        };

        await _sourceRepository.InsertAsync(source, cancellationToken);

        foreach (var speciesId in idList)
        {
            await _sourceRepository.LinkSpeciesAsync(source.Id, speciesId, cancellationToken);
        }

        await _sourceRepository.InsertContentAsync(new PlantSourceContentModel
        {
            SourceID = source.Id,
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
            ParsedAt = now
        }, cancellationToken);

        return (await GetByIdAsync(source.Id, cancellationToken))!;
    }

    private async Task<PlantSourceListItemDto> MapListItemAsync(PlantSourceModel source, CancellationToken cancellationToken)
    {
        var names = await CollectSpeciesNamesAsync(source, cancellationToken);
        return new PlantSourceListItemDto
        {
            Id = source.Id,
            SpeciesId = source.SpeciesID,
            SpeciesName = names.Count == 0 ? null : string.Join("、", names),
            LinkedSpeciesNames = names,
            Title = source.Title,
            Url = source.Url,
            SourceType = source.SourceType,
            ReliabilityLevel = source.ReliabilityLevel,
            Enabled = source.Enabled,
            CreateDate = source.CreateDate
        };
    }

    private async Task<PlantSourceDetailDto> MapDetailAsync(PlantSourceModel source, CancellationToken cancellationToken)
    {
        var contents = await _sourceRepository.GetContentsBySourceIdAsync(source.Id, cancellationToken);
        var latest = contents.OrderByDescending(c => c.ParsedAt ?? c.ModifyDate).FirstOrDefault();
        var names = await CollectSpeciesNamesAsync(source, cancellationToken);

        return new PlantSourceDetailDto
        {
            Id = source.Id,
            SpeciesId = source.SpeciesID,
            SpeciesName = names.Count == 0 ? null : string.Join("、", names),
            LinkedSpeciesNames = names,
            SourceType = source.SourceType,
            Title = source.Title,
            Url = source.Url,
            Domain = source.Domain,
            Author = source.Author,
            ReliabilityLevel = source.ReliabilityLevel,
            ContentHash = source.ContentHash,
            Enabled = source.Enabled,
            CreateDate = source.CreateDate,
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

    private async Task<List<string>> CollectSpeciesNamesAsync(PlantSourceModel source, CancellationToken cancellationToken)
    {
        var linkedIds = await _sourceRepository.GetLinkedSpeciesIdsAsync(source.Id, cancellationToken);
        if (linkedIds.Count == 0) linkedIds = [source.SpeciesID];

        var names = new List<string>();
        foreach (var id in linkedIds.Distinct())
        {
            var species = await _speciesRepository.GetByIdAsync(id, cancellationToken);
            var name = species?.ChineseName ?? species?.CommonName ?? species?.ScientificName;
            if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
        }
        return names.Distinct().ToList();
    }
}

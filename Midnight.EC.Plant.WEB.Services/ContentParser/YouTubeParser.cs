using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Utility.Hash;
using Midnight.EC.Plant.WEB.Utility.Text;
using Midnight.EC.Plant.WEB.Utility.Url;

namespace Midnight.EC.Plant.WEB.Services.ContentParser;

public class YouTubeParser : IPlantContentParser
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<YouTubeParser> _logger;

    public YouTubeParser(IHttpClientFactory httpClientFactory, ILogger<YouTubeParser> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string ParserType => "YouTubeParser";
    public string ParserVersion => "1.0";

    public bool CanParse(Uri uri, SourceType sourceType) =>
        sourceType == SourceType.YouTube ||
        uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
        uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);

    public async Task<ParsedContentDto> ParseAsync(Uri uri, string html, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = UrlNormalizer.Normalize(uri.ToString());
        var title = HtmlTextHelper.ExtractMetaContent(html, "og:title") ?? HtmlTextHelper.ExtractTitle(html);
        var description = HtmlTextHelper.ExtractMetaContent(html, "og:description")
            ?? HtmlTextHelper.ExtractMetaContent(html, "description");
        var author = HtmlTextHelper.ExtractMetaContent(html, "og:site_name") ?? "YouTube";

        var transcript = await TryFetchTranscriptViaOEmbedAsync(normalizedUrl, cancellationToken);
        var cleanText = !string.IsNullOrWhiteSpace(transcript)
            ? $"{title}\n\n{description}\n\nTranscript:\n{transcript}"
            : $"{title}\n\n{description}";

        cleanText = cleanText.Trim();
        var status = string.IsNullOrWhiteSpace(cleanText)
            ? SourceContentStatus.Failed
            : SourceContentStatus.Completed;

        return new ParsedContentDto
        {
            NormalizedUrl = normalizedUrl,
            SourceType = SourceType.YouTube,
            Title = title,
            Author = author,
            Domain = uri.Host,
            RawText = html.Length > 30000 ? html[..30000] : html,
            CleanText = cleanText,
            Summary = HtmlTextHelper.BuildSummary(cleanText),
            Keywords = HtmlTextHelper.ExtractKeywords(cleanText),
            ContentHash = string.IsNullOrWhiteSpace(cleanText) ? null : HashHelper.ComputeSha256(cleanText),
            ParserType = ParserType,
            ParserVersion = ParserVersion,
            ReliabilityLevel = SourceTypeDetector.SuggestReliabilityLevel(SourceType.YouTube),
            Status = status,
            ErrorMessage = status == SourceContentStatus.Failed ? "無法取得 YouTube 影片可用 metadata / transcript。" : null
        };
    }

    private async Task<string?> TryFetchTranscriptViaOEmbedAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ContentFetcher");
            var oembedUrl = $"https://www.youtube.com/oembed?url={Uri.EscapeDataString(url)}&format=json";
            using var response = await client.GetAsync(oembedUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (document.RootElement.TryGetProperty("title", out var titleElement))
            {
                return titleElement.GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YouTube oEmbed fetch failed for {Url}", url);
        }

        return null;
    }
}

public class PlantContentParserFactory : IPlantContentParserFactory
{
    private readonly IEnumerable<IPlantContentParser> _parsers;

    public PlantContentParserFactory(IEnumerable<IPlantContentParser> parsers)
    {
        _parsers = parsers;
    }

    public IPlantContentParser Resolve(Uri uri, SourceType sourceType)
    {
        var parser = _parsers.FirstOrDefault(p => p.CanParse(uri, sourceType) && p.ParserType != "GenericHtmlParser")
            ?? _parsers.First(p => p.ParserType == "GenericHtmlParser");
        return parser;
    }
}

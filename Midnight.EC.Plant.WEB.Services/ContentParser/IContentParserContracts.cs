using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Services.ContentParser;

public interface IPlantContentParser
{
    string ParserType { get; }
    string ParserVersion { get; }
    bool CanParse(Uri uri, SourceType sourceType);
    Task<ParsedContentDto> ParseAsync(Uri uri, string html, CancellationToken cancellationToken = default);
}

public interface IPlantContentParserFactory
{
    IPlantContentParser Resolve(Uri uri, SourceType sourceType);
}

public interface IContentFetcher
{
    Task<string> FetchAsync(string url, CancellationToken cancellationToken = default);
}

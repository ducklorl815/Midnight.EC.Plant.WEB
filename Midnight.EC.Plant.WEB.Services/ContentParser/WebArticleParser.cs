using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Utility.Hash;
using Midnight.EC.Plant.WEB.Utility.Text;
using Midnight.EC.Plant.WEB.Utility.Url;

namespace Midnight.EC.Plant.WEB.Services.ContentParser;

public class GenericHtmlParser : IPlantContentParser
{
    public string ParserType => "GenericHtmlParser";
    public string ParserVersion => "1.0";

    public bool CanParse(Uri uri, SourceType sourceType) => true;

    public Task<ParsedContentDto> ParseAsync(Uri uri, string html, CancellationToken cancellationToken = default)
    {
        var cleanText = HtmlTextHelper.StripHtml(html);
        var title = HtmlTextHelper.ExtractTitle(html) ?? HtmlTextHelper.ExtractMetaContent(html, "og:title");
        var author = HtmlTextHelper.ExtractMetaContent(html, "author");
        var summary = HtmlTextHelper.BuildSummary(cleanText);
        var keywords = HtmlTextHelper.ExtractKeywords(cleanText);

        var result = new ParsedContentDto
        {
            NormalizedUrl = UrlNormalizer.Normalize(uri.ToString()),
            SourceType = SourceTypeDetector.Detect(uri.ToString()),
            Title = title,
            Author = author,
            Domain = uri.Host,
            RawText = html.Length > 50000 ? html[..50000] : html,
            CleanText = cleanText,
            Summary = summary,
            Keywords = keywords,
            ContentHash = string.IsNullOrWhiteSpace(cleanText) ? null : HashHelper.ComputeSha256(cleanText),
            ParserType = ParserType,
            ParserVersion = ParserVersion,
            ReliabilityLevel = SourceTypeDetector.SuggestReliabilityLevel(SourceTypeDetector.Detect(uri.ToString())),
            Status = string.IsNullOrWhiteSpace(cleanText) ? SourceContentStatus.Failed : SourceContentStatus.Completed,
            ErrorMessage = string.IsNullOrWhiteSpace(cleanText) ? "無法抽取有效正文。" : null
        };

        return Task.FromResult(result);
    }
}

public class WebArticleParser : IPlantContentParser
{
    public string ParserType => "WebArticleParser";
    public string ParserVersion => "1.0";

    public bool CanParse(Uri uri, SourceType sourceType) =>
        sourceType is SourceType.Article or SourceType.Blog or SourceType.OfficialWebsite;

    public Task<ParsedContentDto> ParseAsync(Uri uri, string html, CancellationToken cancellationToken = default)
    {
        var articleMatch = System.Text.RegularExpressions.Regex.Match(
            html,
            "<article[\\s\\S]*?</article>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var scopedHtml = articleMatch.Success ? articleMatch.Value : html;
        var cleanText = HtmlTextHelper.StripHtml(scopedHtml);
        var title = HtmlTextHelper.ExtractMetaContent(html, "og:title")
            ?? HtmlTextHelper.ExtractTitle(html);
        var author = HtmlTextHelper.ExtractMetaContent(html, "article:author")
            ?? HtmlTextHelper.ExtractMetaContent(html, "author");
        var description = HtmlTextHelper.ExtractMetaContent(html, "description")
            ?? HtmlTextHelper.ExtractMetaContent(html, "og:description");

        var result = new ParsedContentDto
        {
            NormalizedUrl = UrlNormalizer.Normalize(uri.ToString()),
            SourceType = SourceTypeDetector.Detect(uri.ToString()),
            Title = title,
            Author = author,
            Domain = uri.Host,
            RawText = scopedHtml.Length > 50000 ? scopedHtml[..50000] : scopedHtml,
            CleanText = cleanText,
            Summary = description ?? HtmlTextHelper.BuildSummary(cleanText),
            Keywords = HtmlTextHelper.ExtractKeywords(cleanText),
            ContentHash = string.IsNullOrWhiteSpace(cleanText) ? null : HashHelper.ComputeSha256(cleanText),
            ParserType = ParserType,
            ParserVersion = ParserVersion,
            ReliabilityLevel = SourceTypeDetector.SuggestReliabilityLevel(SourceType.Article),
            Status = string.IsNullOrWhiteSpace(cleanText) ? SourceContentStatus.Failed : SourceContentStatus.Completed,
            ErrorMessage = string.IsNullOrWhiteSpace(cleanText) ? "無法抽取文章正文。" : null
        };

        return Task.FromResult(result);
    }
}

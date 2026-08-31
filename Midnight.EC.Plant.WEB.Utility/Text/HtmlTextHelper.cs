using System.Net;
using System.Text.RegularExpressions;

namespace Midnight.EC.Plant.WEB.Utility.Text;

public static partial class HtmlTextHelper
{
    public static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var withoutScripts = ScriptTagRegex().Replace(html, " ");
        var withoutStyles = StyleTagRegex().Replace(withoutScripts, " ");
        var text = TagRegex().Replace(withoutStyles, " ");
        text = WebUtility.HtmlDecode(text);
        text = WhitespaceRegex().Replace(text, " ").Trim();
        return text;
    }

    public static string? ExtractMetaContent(string html, string propertyName)
    {
        var pattern = $"""<meta[^>]+(?:property|name)=["']{Regex.Escape(propertyName)}["'][^>]+content=["']([^"']+)["']""";
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (match.Success)
        {
            return WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
        }

        pattern = $"""<meta[^>]+content=["']([^"']+)["'][^>]+(?:property|name)=["']{Regex.Escape(propertyName)}["']""";
        match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : null;
    }

    public static string? ExtractTitle(string html)
    {
        var match = TitleRegex().Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : null;
    }

    public static string BuildSummary(string? cleanText, int maxLength = 300)
    {
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            return string.Empty;
        }

        if (cleanText.Length <= maxLength)
        {
            return cleanText;
        }

        return cleanText[..maxLength].Trim() + "…";
    }

    public static string ExtractKeywords(string? cleanText, int take = 8)
    {
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            return string.Empty;
        }

        var words = WordRegex().Matches(cleanText.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(w => w.Length >= 2)
            .Where(w => !StopWords.Contains(w))
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(take)
            .Select(g => g.Key);

        return string.Join(", ", words);
    }

    private static readonly HashSet<string> StopWords =
    [
        "的", "了", "是", "在", "和", "与", "及", "或", "the", "and", "for", "with", "this", "that", "from", "have", "has", "are", "was", "were"
    ];

    [GeneratedRegex("<script[^>]*>[\\s\\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex("<style[^>]*>[\\s\\S]*?</style>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleTagRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("<title[^>]*>([\\s\\S]*?)</title>", RegexOptions.IgnoreCase)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("[\\p{L}\\p{N}]+")]
    private static partial Regex WordRegex();
}

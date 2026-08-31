using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Services.ContentParser;

public static class SourceTypeDetector
{
    public static SourceType Detect(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return SourceType.Other;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return SourceType.Other;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host.Contains("youtube.com") || host.Contains("youtu.be"))
        {
            return SourceType.YouTube;
        }

        if (host.Contains("vimeo.com") || host.Contains("bilibili.com"))
        {
            return SourceType.Video;
        }

        if (host.Contains("blog") || host.Contains("medium.com") || host.Contains("wordpress"))
        {
            return SourceType.Blog;
        }

        if (host.Contains("forum") || host.Contains("reddit.com") || host.Contains("ptt.cc"))
        {
            return SourceType.Forum;
        }

        if (host.Contains("facebook.com") || host.Contains("instagram.com") || host.Contains("twitter.com") || host.Contains("x.com"))
        {
            return SourceType.SocialMedia;
        }

        if (uri.AbsolutePath.EndsWith(".html") || uri.AbsolutePath.Contains("article") || uri.AbsolutePath.Contains("news"))
        {
            return SourceType.Article;
        }

        return SourceType.OfficialWebsite;
    }

    public static int SuggestReliabilityLevel(SourceType sourceType) => sourceType switch
    {
        SourceType.PlantApi => 2,
        SourceType.OfficialWebsite => 1,
        SourceType.Article => 3,
        SourceType.Video or SourceType.YouTube => 4,
        SourceType.Blog => 4,
        SourceType.Forum or SourceType.SocialMedia => 5,
        _ => 3
    };
}

using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Midnight.EC.Plant.WEB.Services.ContentParser;

public class ContentFetcher : IContentFetcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ContentFetcher> _logger;

    public ContentFetcher(IHttpClientFactory httpClientFactory, ILogger<ContentFetcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("ContentFetcher");
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("MidnightECPlantBot/1.0");

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Content fetch failed for {Url} with status {StatusCode}", url, response.StatusCode);
            throw new InvalidOperationException($"無法取得 URL 內容（HTTP {(int)response.StatusCode}）。");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}

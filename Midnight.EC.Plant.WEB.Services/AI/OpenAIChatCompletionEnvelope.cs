using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Services.Configuration;

namespace Midnight.EC.Plant.WEB.Services.AI;

public sealed class OpenAIChatCompletionEnvelope : IChatCompletionEnvelope
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAIChatCompletionEnvelope> _logger;

    public OpenAIChatCompletionEnvelope(
        IHttpClientFactory httpClientFactory,
        IOptions<AiOptions> options,
        ILogger<OpenAIChatCompletionEnvelope> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(
        IReadOnlyList<ChatCompletionMessage> messages,
        ChatCompletionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey))
            throw new ChatCompletionException("尚未設定 AI:OpenAI:ApiKey。");

        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Count == 0)
            throw new ChatCompletionException("聊天訊息不可為空。");

        request ??= new ChatCompletionRequest();
        var payload = new Dictionary<string, object?>
        {
            ["model"] = string.IsNullOrWhiteSpace(request.Model) ? _options.OpenAI.Model : request.Model,
            ["messages"] = messages.Select(ToWireMessage).ToArray()
        };
        if (request.JsonObject)
            payload["response_format"] = new { type = "json_object" };

        var client = _httpClientFactory.CreateClient("OpenAI");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.OpenAI.ApiKey.Trim());
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = Truncate(ExtractError(body) ?? body, 240);
            _logger.LogWarning("OpenAI chat failed: {Status} {Detail}", (int)response.StatusCode, detail);
            throw new ChatCompletionException(
                $"OpenAI HTTP {(int)response.StatusCode}：{detail}",
                (int)response.StatusCode,
                body);
        }

        using var document = JsonDocument.Parse(body);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        content = StripFence(content);
        if (string.IsNullOrWhiteSpace(content))
            throw new ChatCompletionException("OpenAI 回傳內容為空。");

        return content;
    }

    public static string? StripFence(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        var trimmed = content.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return trimmed;

        var lines = trimmed.Split('\n');
        if (lines.Length < 3)
            return trimmed;

        return string.Join('\n', lines.Skip(1).TakeWhile(l => !l.TrimStart().StartsWith("```", StringComparison.Ordinal))).Trim();
    }

    private static object ToWireMessage(ChatCompletionMessage message)
    {
        if (message.Parts is { Count: > 0 })
        {
            var parts = message.Parts.Select(part =>
                string.Equals(part.Type, "image_url", StringComparison.OrdinalIgnoreCase)
                    ? (object)new { type = "image_url", image_url = new { url = part.ImageUrl } }
                    : new { type = "text", text = part.Text ?? string.Empty }).ToArray();
            return new { role = message.Role, content = parts };
        }

        return new { role = message.Role, content = message.Text ?? string.Empty };
    }

    private static string? ExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                    return message.GetString();
                return error.ToString();
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}

namespace Midnight.EC.Plant.WEB.Services.AI;

public interface IChatCompletionEnvelope
{
    Task<string> CompleteAsync(
        IReadOnlyList<ChatCompletionMessage> messages,
        ChatCompletionRequest? request = null,
        CancellationToken cancellationToken = default);
}

public sealed class ChatCompletionMessage
{
    public required string Role { get; init; }
    public string? Text { get; init; }
    public IReadOnlyList<ChatContentPart>? Parts { get; init; }
}

public sealed class ChatContentPart
{
    public string Type { get; init; } = "text";
    public string? Text { get; init; }
    public string? ImageUrl { get; init; }

    public static ChatContentPart FromText(string text) => new() { Type = "text", Text = text };

    public static ChatContentPart FromImageDataUrl(string dataUrl) =>
        new() { Type = "image_url", ImageUrl = dataUrl };
}

public sealed class ChatCompletionRequest
{
    public bool JsonObject { get; init; } = true;
    public string? Model { get; init; }
}

public sealed class ChatCompletionException : InvalidOperationException
{
    public ChatCompletionException(string message, int? statusCode = null, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public int? StatusCode { get; }
    public string? ResponseBody { get; }
}

using Midnight.EC.Plant.WEB.Services.AI;

namespace Midnight.EC.Plant.WEB.Tests;

internal sealed class FakeChatCompletionEnvelope : IChatCompletionEnvelope
{
    public string Response { get; set; } = "{}";
    public Exception? Error { get; set; }
    public List<IReadOnlyList<ChatCompletionMessage>> Calls { get; } = [];

    public Task<string> CompleteAsync(
        IReadOnlyList<ChatCompletionMessage> messages,
        ChatCompletionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(messages);
        if (Error != null)
            throw Error;
        return Task.FromResult(Response);
    }
}

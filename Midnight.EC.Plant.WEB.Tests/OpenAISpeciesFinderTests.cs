using Microsoft.Extensions.Logging.Abstractions;
using Midnight.EC.Plant.WEB.Services.AI;

namespace Midnight.EC.Plant.WEB.Tests;

public class OpenAISpeciesFinderTests
{
    [Fact]
    public async Task FindByText_parses_candidates_and_keeps_trusted_chinese()
    {
        var chat = new FakeChatCompletionEnvelope
        {
            Response = """
                {
                  "candidates": [
                    {
                      "scientificName": "Monstera deliciosa",
                      "commonName": "Swiss cheese plant",
                      "genus": "Monstera",
                      "family": "Araceae",
                      "alsoKnownAs": ["蓬莱蕉"],
                      "identificationHint": "裂葉與穿孔"
                    }
                  ]
                }
                """
        };
        var finder = new OpenAISpeciesFinderService(chat, NullLogger<OpenAISpeciesFinderService>.Instance);

        var list = await finder.FindByTextAsync("龜背芋");

        Assert.Single(list);
        Assert.Equal("Monstera deliciosa", list[0].ScientificName);
        Assert.Equal("龜背芋", list[0].ChineseName);
    }
}

public class ChatEnvelopeFenceTests
{
    [Fact]
    public void StripFence_removes_markdown()
    {
        var raw = "```json\n{\"ok\":true}\n```";
        Assert.Equal("{\"ok\":true}", OpenAIChatCompletionEnvelope.StripFence(raw));
    }

    [Fact]
    public void StripFence_leaves_plain_json()
    {
        Assert.Equal("{\"ok\":true}", OpenAIChatCompletionEnvelope.StripFence("{\"ok\":true}"));
    }
}

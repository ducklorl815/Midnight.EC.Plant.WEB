using Microsoft.Extensions.Logging.Abstractions;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Services.AI;

namespace Midnight.EC.Plant.WEB.Tests;

public class EnvironmentFitServiceTests
{
    [Fact]
    public async Task Refresh_parses_fit_json()
    {
        var chat = new FakeChatCompletionEnvelope
        {
            Response = """
                {
                  "fitSummary": "散射需求與窗邊半日照大致契合。",
                  "problems": [ { "text": "午後直射可能灼葉", "hint": "加紗簾" } ],
                  "adjustments": [ "移到東向窗" ],
                  "fertilizerAdjustment": "生長季薄肥即可"
                }
                """
        };
        var fit = new EnvironmentFitService(chat, NullLogger<EnvironmentFitService>.Instance);

        var result = await fit.RefreshAsync(
            "窗邊龜背",
            "Monstera deliciosa",
            new ExternalKnowledgeResult { LightRequirement = "散射" },
            new PlantEnvironmentContext { LightLabel = "半日照", PlacementLabel = "窗邊" });

        Assert.True(result.Succeeded);
        Assert.Contains("fitSummary", result.Advice, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_skips_when_environment_empty()
    {
        var chat = new FakeChatCompletionEnvelope { Response = "{}" };
        var fit = new EnvironmentFitService(chat, NullLogger<EnvironmentFitService>.Instance);

        var result = await fit.RefreshAsync("x", null, new ExternalKnowledgeResult(), new PlantEnvironmentContext());

        Assert.True(result.SkippedNoEnvironment);
        Assert.Empty(chat.Calls);
    }
}

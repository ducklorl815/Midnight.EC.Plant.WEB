using Microsoft.Extensions.Logging.Abstractions;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Services.AI;

namespace Midnight.EC.Plant.WEB.Tests;

public class CareKnowledgeSynthesisTests
{
    [Fact]
    public async Task Synthesize_parses_v3_modules_from_envelope()
    {
        var chat = new FakeChatCompletionEnvelope
        {
            Response = """
                {
                  "chineseName": "龜背芋",
                  "scientificName": "Monstera deliciosa",
                  "quickFacts": {
                    "light": "Diffuse",
                    "wateringStrategy": "dry_then_soak",
                    "humidity": "medium",
                    "temperatureMin": 16,
                    "temperatureMax": 28,
                    "growingSeason": ["spring","summer"]
                  },
                  "modules": [
                    { "id": "identification", "content": "大型裂葉，氣生根明顯。" },
                    { "id": "light", "content": "明亮散射光。" },
                    { "id": "watering", "content": "介質乾透再澆。" }
                  ]
                }
                """
        };
        var synth = new CareKnowledgeSynthesisService(chat, NullLogger<CareKnowledgeSynthesisService>.Instance);

        var result = await synth.SynthesizeAsync("龜背芋", "Monstera deliciosa");

        Assert.Equal(CareSynthesisAttemptStatus.Succeeded, result.Status);
        Assert.NotNull(result.Partial);
        Assert.False(string.IsNullOrWhiteSpace(result.Partial!.ExternalCareGuide));
        Assert.Contains("identification", result.Partial.ExternalCareGuide, StringComparison.Ordinal);
        Assert.Single(chat.Calls);
    }

    [Fact]
    public async Task Synthesize_maps_envelope_failure()
    {
        var chat = new FakeChatCompletionEnvelope
        {
            Error = new ChatCompletionException("尚未設定 AI:OpenAI:ApiKey。")
        };
        var synth = new CareKnowledgeSynthesisService(chat, NullLogger<CareKnowledgeSynthesisService>.Instance);

        var result = await synth.SynthesizeAsync("龜背芋", "Monstera deliciosa");

        Assert.Equal(CareSynthesisAttemptStatus.ServiceFailed, result.Status);
        Assert.Contains("ApiKey", result.FailureReason);
    }
}

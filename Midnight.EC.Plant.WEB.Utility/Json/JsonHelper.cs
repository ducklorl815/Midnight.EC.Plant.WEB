using System.Text.Json;
using System.Text.Json.Serialization;

namespace Midnight.EC.Plant.WEB.Utility.Json;

public static class JsonHelper
{
    public static readonly JsonSerializerOptions DefaultOptions = CreateDefaultOptions();

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        options.Converters.Add(new ListStringJsonConverter());
        return options;
    }

    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, DefaultOptions);

    public static T? Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, DefaultOptions);
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Midnight.EC.Plant.WEB.Models.AI;

/// <summary>
/// Accepts object { light, water, humidity, temperature }, a freeform string, or null → empty DTO.
/// </summary>
public sealed class PlantEnvironmentAssessmentJsonConverter : JsonConverter<PlantEnvironmentAssessmentDto>
{
    public override PlantEnvironmentAssessmentDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return new();

            case JsonTokenType.String:
            {
                var value = reader.GetString();
                return string.IsNullOrWhiteSpace(value)
                    ? new()
                    : new() { Light = value.Trim() };
            }

            case JsonTokenType.StartObject:
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;
                return new PlantEnvironmentAssessmentDto
                {
                    Light = ReadField(root, "light"),
                    Water = ReadField(root, "water"),
                    Humidity = ReadField(root, "humidity"),
                    Temperature = ReadField(root, "temperature")
                };
            }

            default:
                using (JsonDocument.ParseValue(ref reader))
                {
                }

                return new();
        }
    }

    public override void Write(Utf8JsonWriter writer, PlantEnvironmentAssessmentDto value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("light", value.Light ?? string.Empty);
        writer.WriteString("water", value.Water ?? string.Empty);
        writer.WriteString("humidity", value.Humidity ?? string.Empty);
        writer.WriteString("temperature", value.Temperature ?? string.Empty);
        writer.WriteEndObject();
    }

    private static string ReadField(JsonElement root, string name)
    {
        foreach (var prop in root.EnumerateObject())
        {
            if (!prop.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString()?.Trim() ?? string.Empty,
                JsonValueKind.Null => string.Empty,
                JsonValueKind.Number => prop.Value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => prop.Value.ToString()
            };
        }

        return string.Empty;
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Midnight.EC.Plant.WEB.Utility.Json;

/// <summary>
/// Accepts JSON string arrays, a single string (wrapped as one element), or null/empty → [].
/// </summary>
public sealed class ListStringJsonConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return [];

            case JsonTokenType.String:
            {
                var value = reader.GetString();
                return string.IsNullOrWhiteSpace(value) ? [] : [value.Trim()];
            }

            case JsonTokenType.StartArray:
            {
                var list = new List<string>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.Null)
                    {
                        continue;
                    }

                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var item = reader.GetString();
                        if (!string.IsNullOrWhiteSpace(item))
                        {
                            list.Add(item.Trim());
                        }

                        continue;
                    }

                    // Non-string array element: skip without failing the whole payload.
                    using var skipped = JsonDocument.ParseValue(ref reader);
                }

                return list;
            }

            default:
                // Unexpected scalar/object: skip and return empty rather than throw.
                using (JsonDocument.ParseValue(ref reader))
                {
                }

                return [];
        }
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value ?? [])
        {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();
    }
}

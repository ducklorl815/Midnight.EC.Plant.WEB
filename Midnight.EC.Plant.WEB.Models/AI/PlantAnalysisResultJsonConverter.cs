using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Midnight.EC.Plant.WEB.Models.External;

namespace Midnight.EC.Plant.WEB.Models.AI;

/// <summary>
/// Lenient parser for LLM JSON: tolerates string/number/bool mismatches instead of failing the whole analysis.
/// </summary>
public sealed class PlantAnalysisResultJsonConverter : JsonConverter<PlantAnalysisResultDto>
{
    public override PlantAnalysisResultDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new();
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        return Parse(doc.RootElement);
    }

    public override void Write(Utf8JsonWriter writer, PlantAnalysisResultDto value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("summary", value.Summary ?? string.Empty);
        writer.WriteNumber("healthScore", value.HealthScore);
        WriteStringArray(writer, "observations", value.Observations);
        WriteStringArray(writer, "possibleIssues", value.PossibleIssues);

        writer.WritePropertyName("nutrientHypotheses");
        writer.WriteStartArray();
        foreach (var h in value.NutrientHypotheses ?? [])
        {
            writer.WriteStartObject();
            writer.WriteString("nutrient", h.Nutrient ?? string.Empty);
            writer.WriteString("likelihood", h.Likelihood ?? string.Empty);
            if (h.VisualClues != null)
            {
                writer.WriteString("visualClues", h.VisualClues);
            }

            writer.WriteString("caveat", h.Caveat ?? string.Empty);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WritePropertyName("environmentAssessment");
        JsonSerializer.Serialize(writer, value.EnvironmentAssessment ?? new(), options);

        WriteStringArray(writer, "recommendations", value.Recommendations);
        WriteStringArray(writer, "warning", value.Warning);

        writer.WritePropertyName("citations");
        writer.WriteStartArray();
        foreach (var c in value.Citations ?? [])
        {
            writer.WriteStartObject();
            writer.WriteString("sourceTitle", c.SourceTitle ?? string.Empty);
            writer.WriteString("sourceUrl", c.SourceUrl ?? string.Empty);
            writer.WriteNumber("reliabilityLevel", c.ReliabilityLevel);
            if (c.UsedFor != null)
            {
                writer.WriteString("usedFor", c.UsedFor);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteString("growthTrend", value.GrowthTrend);
        writer.WriteString("wateringAdvice", value.WateringAdvice);
        writer.WriteString("pestRisk", value.PestRisk);
        WriteStringArray(writer, "alerts", value.Alerts);
        writer.WriteNumber("confidence", value.Confidence);
        writer.WriteBoolean("needsHumanReview", value.NeedsHumanReview);

        if (value.FertilizerAdvice != null)
        {
            writer.WritePropertyName("fertilizerAdvice");
            JsonSerializer.Serialize(writer, value.FertilizerAdvice, options);
        }

        writer.WriteEndObject();
    }

    public static PlantAnalysisResultDto Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return new();
        }

        return new PlantAnalysisResultDto
        {
            Summary = GetString(root, "summary") ?? string.Empty,
            HealthScore = NormalizeHealthScore(GetOptionalDecimal(root, "healthScore")),
            Observations = ParseStringList(root, "observations"),
            PossibleIssues = ParseStringList(root, "possibleIssues"),
            NutrientHypotheses = ParseNutrientHypotheses(root, "nutrientHypotheses"),
            EnvironmentAssessment = ParseEnvironment(root, "environmentAssessment"),
            Recommendations = ParseStringList(root, "recommendations"),
            Warning = ParseStringList(root, "warning"),
            Citations = ParseCitations(root, "citations"),
            GrowthTrend = GetString(root, "growthTrend"),
            WateringAdvice = GetString(root, "wateringAdvice"),
            PestRisk = GetString(root, "pestRisk"),
            Alerts = ParseStringList(root, "alerts"),
            Confidence = NormalizeConfidence(GetOptionalDecimal(root, "confidence")),
            NeedsHumanReview = GetOptionalBool(root, "needsHumanReview") ?? false,
            FertilizerAdvice = ParseFertilizer(root, "fertilizerAdvice")
        };
    }

    private static List<NutrientHypothesisDto> ParseNutrientHypotheses(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el))
        {
            return [];
        }

        if (el.ValueKind == JsonValueKind.String)
        {
            var text = el.GetString();
            return string.IsNullOrWhiteSpace(text)
                ? []
                : [new NutrientHypothesisDto { Nutrient = text.Trim(), Likelihood = "低", Caveat = "單憑照片無法確診。" }];
        }

        if (el.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<NutrientHypothesisDto>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var text = item.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    list.Add(new NutrientHypothesisDto
                    {
                        Nutrient = text.Trim(),
                        Likelihood = "低",
                        Caveat = "單憑照片無法確診。"
                    });
                }

                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var nutrient = GetString(item, "nutrient");
            if (string.IsNullOrWhiteSpace(nutrient))
            {
                continue;
            }

            list.Add(new NutrientHypothesisDto
            {
                Nutrient = nutrient,
                Likelihood = GetString(item, "likelihood") ?? "中",
                VisualClues = GetString(item, "visualClues"),
                Caveat = GetString(item, "caveat") ?? "單憑照片無法確診。"
            });
        }

        return list;
    }

    private static void WriteStringArray(Utf8JsonWriter writer, string name, List<string>? values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var item in values ?? [])
        {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();
    }

    private static PlantEnvironmentAssessmentDto ParseEnvironment(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el))
        {
            return new();
        }

        return el.ValueKind switch
        {
            JsonValueKind.Object => new PlantEnvironmentAssessmentDto
            {
                Light = GetString(el, "light") ?? string.Empty,
                Water = GetString(el, "water") ?? string.Empty,
                Humidity = GetString(el, "humidity") ?? string.Empty,
                Temperature = GetString(el, "temperature") ?? string.Empty
            },
            JsonValueKind.String => string.IsNullOrWhiteSpace(el.GetString())
                ? new()
                : new() { Light = el.GetString()!.Trim() },
            _ => new()
        };
    }

    private static List<PlantCitationResultDto> ParseCitations(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<PlantCitationResultDto>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            list.Add(new PlantCitationResultDto
            {
                SourceTitle = GetString(item, "sourceTitle") ?? string.Empty,
                SourceUrl = GetString(item, "sourceUrl") ?? string.Empty,
                ReliabilityLevel = GetOptionalInt(item, "reliabilityLevel") ?? 0,
                UsedFor = GetString(item, "usedFor")
            });
        }

        return list;
    }

    private static FertilizerRecipeDto? ParseFertilizer(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el))
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.String)
        {
            var notes = el.GetString()?.Trim();
            return string.IsNullOrWhiteSpace(notes) ? null : new FertilizerRecipeDto { Notes = notes };
        }

        if (el.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var recipe = new FertilizerRecipeDto
        {
            Type = NullIfEmpty(GetString(el, "type")),
            NpkHint = NullIfEmpty(GetString(el, "npkHint")),
            Dilution = NullIfEmpty(GetString(el, "dilution")),
            DilutionStrong = GetOptionalInt(el, "dilutionStrong"),
            DilutionMild = GetOptionalInt(el, "dilutionMild"),
            Frequency = NullIfEmpty(GetString(el, "frequency")),
            Notes = NullIfEmpty(GetString(el, "notes"))
        };

        if (string.IsNullOrWhiteSpace(recipe.Type)
            && string.IsNullOrWhiteSpace(recipe.Dilution)
            && string.IsNullOrWhiteSpace(recipe.Frequency)
            && string.IsNullOrWhiteSpace(recipe.Notes)
            && string.IsNullOrWhiteSpace(recipe.NpkHint))
        {
            return null;
        }

        return recipe;
    }

    private static List<string> ParseStringList(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el))
        {
            return [];
        }

        if (el.ValueKind == JsonValueKind.String)
        {
            var value = el.GetString();
            return string.IsNullOrWhiteSpace(value) ? [] : [value.Trim()];
        }

        if (el.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return el.EnumerateArray()
            .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToList();
    }

    private static int NormalizeHealthScore(decimal? raw)
    {
        if (!raw.HasValue)
        {
            return 0;
        }

        var score = (int)Math.Round(raw.Value, MidpointRounding.AwayFromZero);
        return Math.Clamp(score, 0, 100);
    }

    private static decimal NormalizeConfidence(decimal? raw)
    {
        if (!raw.HasValue)
        {
            return 0m;
        }

        var value = raw.Value;
        // LLM often returns 0–100 percentage instead of 0–1.
        if (value > 1m && value <= 100m)
        {
            value /= 100m;
        }

        return Math.Clamp(value, 0m, 1m);
    }

    private static string? GetString(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString()?.Trim(),
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => el.ToString()
        };
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal? GetOptionalDecimal(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Number when el.TryGetDecimal(out var n) => n,
            JsonValueKind.String => ParseDecimalLoose(el.GetString()),
            _ => ParseDecimalLoose(el.ToString())
        };
    }

    private static int? GetOptionalInt(JsonElement root, string name)
    {
        var d = GetOptionalDecimal(root, name);
        return d.HasValue ? (int)Math.Round(d.Value, MidpointRounding.AwayFromZero) : null;
    }

    private static bool? GetOptionalBool(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when el.TryGetInt32(out var n) => n != 0,
            JsonValueKind.String => ParseBoolLoose(el.GetString()),
            _ => null
        };
    }

    private static bool? ParseBoolLoose(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim();
        if (bool.TryParse(text, out var b))
        {
            return b;
        }

        return text.ToLowerInvariant() switch
        {
            "1" or "yes" or "y" or "是" or "真" or "需要" => true,
            "0" or "no" or "n" or "否" or "假" or "不需要" => false,
            _ => null
        };
    }

    private static decimal? ParseDecimalLoose(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim();
        if (text is "null" or "undefined" or "-" or "未知" or "N/A" or "n/a")
        {
            return null;
        }

        var match = Regex.Match(text, @"-?\d+(\.\d+)?");
        if (!match.Success)
        {
            return null;
        }

        return decimal.TryParse(match.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string name, out JsonElement value)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        if (root.TryGetProperty(name, out value))
        {
            return true;
        }

        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}

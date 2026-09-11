using System.Text.RegularExpressions;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Services.External;

namespace Midnight.EC.Plant.WEB.Services.PageComposer;

public static class PlantDetailWallMapper
{
    private static readonly Regex NamePrefixBeforeShi = new(
        @"^[\p{L}\p{N}\s.\-×',]+(?:（[^）]+）|\([^)]+\))?\s*(?=是)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string ResolveIntro(PlantModel plant)
    {
        var knowledge = plant.Species?.Knowledge;
        var guide = CareGuideJson.TryParseSpeciesGuide(knowledge?.ExternalCareGuide);
        string? raw = guide?.GetWallIntro();

        if (string.IsNullOrWhiteSpace(raw) && !string.IsNullOrWhiteSpace(knowledge?.CareSummary))
            raw = knowledge.CareSummary.Trim();
        else if (string.IsNullOrWhiteSpace(raw) && !string.IsNullOrWhiteSpace(knowledge?.ExternalCareGuide)
            && !CareGuideJson.LooksLikeJson(knowledge.ExternalCareGuide))
        {
            var plain = ExternalCareGuideBuilder.SanitizeForDisplay(knowledge.ExternalCareGuide);
            if (!string.IsNullOrWhiteSpace(plain))
                raw = plain;
        }
        else if (string.IsNullOrWhiteSpace(raw) && !string.IsNullOrWhiteSpace(plant.Description))
            raw = plant.Description.Trim();

        if (string.IsNullOrWhiteSpace(raw))
            return "尚無介紹";

        return StripLeadingNamePrefix(raw);
    }

    public static string StripLeadingNamePrefix(string intro)
    {
        var t = intro.Trim();
        var m = NamePrefixBeforeShi.Match(t);
        if (m.Success && m.Length > 0 && m.Length < t.Length)
            return t[m.Length..].TrimStart();
        return t;
    }

    public static string ResolveDisplayName(PlantModel plant)
    {
        if (!string.IsNullOrWhiteSpace(plant.NickName))
            return plant.NickName.Trim();
        if (!string.IsNullOrWhiteSpace(plant.Species?.ChineseName))
            return plant.Species.ChineseName.Trim();
        if (!string.IsNullOrWhiteSpace(plant.Name))
            return plant.Name.Trim();
        return plant.Species?.ScientificName?.Trim() ?? "未命名植栽";
    }

    public static PlantDetailWallSlideDto ToSlide(
        PlantModel plant,
        PlantProfileModel? profile,
        string? coverImagePath,
        double zoom = 1,
        double focusX = 50,
        double focusY = 50)
    {
        _ = profile; // framing / env icons no longer used on the wall copy

        return new PlantDetailWallSlideDto
        {
            PlantId = plant.ID,
            DisplayName = ResolveDisplayName(plant),
            ScientificName = string.IsNullOrWhiteSpace(plant.Species?.ScientificName)
                ? null
                : plant.Species!.ScientificName.Trim(),
            Intro = ResolveIntro(plant),
            CoverImagePath = coverImagePath,
            Zoom = zoom <= 0 ? 1 : zoom,
            FocusX = Math.Clamp(focusX, 0, 100),
            FocusY = Math.Clamp(focusY, 0, 100),
            CareFacts = BuildCareFacts(plant.Species?.Knowledge)
        };
    }

    public static List<PlantDetailWallCareFactDto> BuildCareFacts(PlantKnowledgeModel? knowledge)
    {
        var suggestedLight = CareKnowledgeCompleteness.ResolveSuggestedLight(
            knowledge?.SuggestedLight,
            knowledge?.LightRequirement,
            knowledge?.CareSummary,
            knowledge?.ExternalCareGuide);

        var suggestedDays = knowledge?.SuggestedWateringIntervalDays;
        if (suggestedDays is null or <= 0)
            suggestedDays = InferWateringIntervalDays(knowledge?.WaterRequirement);

        var facts = new List<PlantDetailWallCareFactDto>
        {
            Fact("建議日照", suggestedLight.HasValue ? LightLevelDisplay.ToLabel(suggestedLight) : "未設定"),
            Fact("光照", BlankToUnset(knowledge?.LightRequirement))
        };

        if (suggestedDays is > 0)
            facts.Add(Fact("建議澆水", $"每 {suggestedDays} 天"));
        else
            facts.Add(Fact("澆水", BlankToUnset(knowledge?.WaterRequirement)));

        facts.Add(Fact("濕度", BlankToUnset(knowledge?.HumidityRequirement)));
        facts.Add(Fact("溫度", FormatTemperature(knowledge?.TemperatureMin, knowledge?.TemperatureMax) ?? "未設定"));
        facts.Add(Fact("土壤", BlankToUnset(knowledge?.SoilRequirement)));
        facts.Add(Fact("施肥", BlankToUnset(knowledge?.FertilizerRequirement)));
        facts.Add(Fact("生長季", BlankToUnset(knowledge?.GrowthSeason)));
        return facts;
    }

    private static PlantDetailWallCareFactDto Fact(string label, string value) => new()
    {
        Label = label,
        Value = value
    };

    private static string BlankToUnset(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "未設定" : value.Trim();

    private static string? FormatTemperature(decimal? min, decimal? max)
    {
        if (min == null && max == null)
            return null;
        return $"{min}°C ~ {max}°C";
    }

    private static int? InferWateringIntervalDays(string? waterRequirement)
    {
        if (string.IsNullOrWhiteSpace(waterRequirement))
            return null;

        var match = Regex.Match(waterRequirement, @"(\d+)\s*(天|日|day)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var days))
            return Math.Clamp(days, 1, 90);

        return null;
    }
}

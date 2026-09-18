namespace Midnight.EC.Plant.WEB.Services.PlantEffect;

public sealed class PlantVisualComposition
{
    public string Layout { get; set; } = "LayoutE";
    public string Style { get; set; } = "BotanicalEditorial";
    public string ColorPalette { get; set; } = "SageCreamEarth";
    public string Decoration { get; set; } = "TornPaperTapeBrush";
    public string DoodlePosition { get; set; } = "Corners";
    public string InfoHintPosition { get; set; } = "Left";
    public string TypographyPosition { get; set; } = "TopLeft";
    public string GlitchPosition { get; set; } = "BottomEdge";
    public string Crop { get; set; } = "CenteredPortrait";
    public string PaperTexture { get; set; } = "CreamWashi";
    public string BrushStyle { get; set; } = "InkWash";
    public string PaletteReason { get; set; } = "default";
    /// <summary>Hero plant photo horizontal anchor: Left | Center | Right.</summary>
    public string HeroPosition { get; set; } = "Center";
    /// <summary>Wall display aspect hint: 16:9 | 3:4 | 4:5.</summary>
    public string WallAspect { get; set; } = "16:9";

    public string ToDecorationJson() => System.Text.Json.JsonSerializer.Serialize(this);
}

public static class PlantVisualCompositionService
{
    private static readonly string[] Layouts =
    [
        "LayoutA_LeftInfo_CenterPhoto_RightDetail",
        "LayoutB_RightInfo_LeftPhoto_TopTitle",
        "LayoutC_TopTitle_CenterPhoto_BottomLeftInfo",
        "LayoutD_TopLeftInfo_CenterPhoto_BottomRightAnnotations",
        "LayoutE_LargePhoto_SmallInfoCard_SurroundingDoodles"
    ];

    private static readonly string[] Styles =
    [
        "BotanicalEditorial",
        "BotanicalScrapbook",
        "VintagePlantJournal",
        "ModernBotanicalPoster",
        "NaturalMagazine",
        "SpecimenNotebook",
        "HandDrawnBotanical",
        "MinimalBotanical",
        "BotanicalGlitch"
    ];

    private static readonly string[] Decorations =
    [
        "TornPaperTapeBrush",
        "SpecimenLabelsArrows",
        "InkSplatterWashi",
        "PressedLeafCollage",
        "NotebookMarginMarks",
        "BrushStrokeFrames"
    ];

    private static readonly string[] DoodlePositions =
    [
        "Corners", "LeftMargin", "RightMargin", "AroundSubject", "BottomBand"
    ];

    private static readonly string[] InfoPositions =
    [
        "Left", "Right", "TopLeft", "BottomLeft", "FloatingSmallCard"
    ];

    private static readonly string[] TypographyPositions =
    [
        "TopLeft", "TopCenter", "AlongLeftEdge", "BottomRight"
    ];

    private static readonly string[] GlitchPositions =
    [
        "BottomEdge", "TopRightCorner", "ThinHorizontalBand", "LocalCornerOnly"
    ];

    private static readonly string[] Crops =
    [
        "CenteredPortrait", "SlightLeftBias", "SlightRightBias", "CloserDetailCrop", "WiderEnvironmentalCrop"
    ];

    private static readonly string[] Papers =
    [
        "CreamWashi", "RecycledKraft", "SoftIvoryJournal", "SpeckledSketchbook"
    ];

    private static readonly string[] Brushes =
    [
        "InkWash", "DryBrush", "PencilContour", "WatercolorEdge"
    ];

    private static readonly string[] HeroPositions = ["Left", "Center", "Right"];

    private static readonly string[] WallAspects = ["16:9", "3:4", "4:5"];

    /// <summary>
    /// Fixed plant-hero preset for the main generation path (no left knowledge sticky).
    /// </summary>
    public const string GoldenReferencePresetVersion = "PlantHeroPresetV1";

    public static PlantVisualComposition CreateGoldenReferencePreset(
        string? plantDisplayName,
        string? scientificName,
        string? careSummary)
    {
        var palette = ResolvePalette(plantDisplayName, scientificName, careSummary);

        return new PlantVisualComposition
        {
            // Large plant photo with surrounding doodles — no left care card
            Layout = "LayoutE_LargePhoto_SmallInfoCard_SurroundingDoodles",
            Style = "BotanicalScrapbook",
            ColorPalette = palette.Name,
            PaletteReason = $"{palette.Reason}|{GoldenReferencePresetVersion}",
            Decoration = "TornPaperTapeBrush",
            DoodlePosition = "AroundSubject",
            InfoHintPosition = "FloatingSmallCard",
            TypographyPosition = "TopLeft",
            GlitchPosition = "LocalCornerOnly",
            Crop = "CloserDetailCrop",
            PaperTexture = "CreamWashi",
            BrushStyle = "DryBrush",
            HeroPosition = "Center",
            WallAspect = "16:9"
        };
    }

    /// <summary>
    /// Legacy randomized composition. Kept for optional fallback / experiments;
    /// main Generate path must use <see cref="CreateGoldenReferencePreset"/>.
    /// </summary>
    public static PlantVisualComposition CreateRandom(string? plantDisplayName, string? scientificName, string? careSummary)
    {
        var rng = Random.Shared;
        var style = Pick(Styles, rng);
        // Glitch style must stay subtle even when selected
        var glitch = style == "BotanicalGlitch"
            ? "LocalCornerOnly"
            : Pick(GlitchPositions, rng);

        var palette = ResolvePalette(plantDisplayName, scientificName, careSummary);
        var hero = Pick(HeroPositions, rng);

        return new PlantVisualComposition
        {
            Layout = Pick(Layouts, rng),
            Style = style,
            ColorPalette = palette.Name,
            PaletteReason = palette.Reason,
            Decoration = Pick(Decorations, rng),
            DoodlePosition = Pick(DoodlePositions, rng),
            InfoHintPosition = Pick(InfoPositions, rng),
            TypographyPosition = Pick(TypographyPositions, rng),
            GlitchPosition = glitch,
            Crop = Pick(Crops, rng),
            PaperTexture = Pick(Papers, rng),
            BrushStyle = Pick(Brushes, rng),
            HeroPosition = hero,
            WallAspect = Pick(WallAspects, rng)
        };
    }

    private static (string Name, string Reason) ResolvePalette(string? displayName, string? scientificName, string? careSummary)
    {
        var blob = $"{displayName} {scientificName} {careSummary}".ToLowerInvariant();

        if (ContainsAny(blob, "cactus", "succulent", "mammillaria", "仙人掌", "多肉", "玉露", "銀手"))
            return ("OliveSageEarthCream", "succulent-cactus");

        if (ContainsAny(blob, "flower", "bloom", "orchid", "花", "蘭"))
            return ("MutedFloralAccent_CreamSage", "floral");

        if (ContainsAny(blob, "fern", "forest", "dark green", "蕨", "深綠", "觀葉"))
            return ("ForestDarkGreenCream", "deep-green");

        return ("SageMossCreamEarth", "default-botanical");
    }

    private static bool ContainsAny(string haystack, params string[] needles) =>
        needles.Any(n => haystack.Contains(n, StringComparison.Ordinal));

    private static string Pick(string[] items, Random rng) => items[rng.Next(items.Length)];
}

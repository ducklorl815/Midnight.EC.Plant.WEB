using System.Text;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Services.PlantEffect;

public static class PlantEffectPromptBuilder
{
    /// <summary>
    /// Plant-hero scrapbook base — no left knowledge sticky / care card zone.
    /// </summary>
    public const string PromptVersion = "PlantEffectPromptV6_PlantHero";

    public static string Build(
        PlantModel plant,
        PlantVisualComposition composition,
        PlantEffectOverlayDto overlay)
    {
        _ = plant;
        var scientific = overlay.ScientificName ?? "";
        var hero = string.IsNullOrWhiteSpace(composition.HeroPosition)
            ? "Center"
            : composition.HeroPosition;

        var sb = new StringBuilder();
        sb.AppendLine("Edit the supplied ORIGINAL plant photograph into a Botanical Scrapbook collage BASE image.");
        sb.AppendLine("Focus on the REAL plant as the hero subject. Do NOT reserve or paint a large left blank knowledge-card / sticky-note / care-info panel.");
        sb.AppendLine("Canvas MUST be landscape widescreen (~16:9 / 1536x1024).");
        sb.AppendLine("Overall vibe: soft cream scrapbook page, distressed paint edges, white hand-drawn doodles/arrows/sparkles, green brush-tape art labels, tiny plant illustrations, subtle RGB glitch only in a tiny corner.");
        sb.AppendLine();
        sb.AppendLine("CRITICAL — PRESERVE THE REAL PHOTO:");
        sb.AppendLine("- Keep the real plant identity, morphology, pot and details. Do not invent a different plant.");
        sb.AppendLine($"- Place the real plant photograph as the dominant HERO, anchored toward the {hero.ToUpperInvariant()} of the canvas (HeroPosition={hero}).");
        sb.AppendLine("- Fill most of the frame with the plant and scrapbook decorations around it; do not stretch the plant unnaturally.");
        sb.AppendLine("- Do NOT leave a large empty left third for text cards.");
        sb.AppendLine();
        sb.AppendLine("CRITICAL — LAYOUT (plant-hero, no knowledge sticky):");
        sb.AppendLine("- HERO: large plant photograph (torn-paper or taped photo edges OK).");
        sb.AppendLine("- OPTIONAL: one small Polaroid / macro detail inset near a corner (not stacked over the plant face).");
        sb.AppendLine("- OPTIONAL: tiny botanical doodles, sparkles, arrows pointing at plant features.");
        sb.AppendLine("- OPTIONAL: brush-stroke art labels (shape only) and decorative lettering silhouettes — no readable care facts.");
        sb.AppendLine("- TOP: light room for a decorative title flourish if needed, but keep the plant dominant.");
        sb.AppendLine("- FORBIDDEN: left-side torn-paper knowledge card, sticky note, specimen fact sheet, or large blank paper panel for care text.");
        sb.AppendLine();
        sb.AppendLine("CRITICAL — FRAME & BACKGROUND:");
        sb.AppendLine("- Final image must have clearly ROUND CORNERS on all four corners (smooth border-radius look, about 24–40px feel).");
        sb.AppendLine("- Any empty / margin / letterbox areas must be WHITE or warm cream paper — NEVER black, NEVER dark void.");
        sb.AppendLine("- Prefer a white/cream scrapbook page background with visible paper texture.");
        sb.AppendLine();
        sb.AppendLine("TEXT:");
        sb.AppendLine("- Do NOT render readable Chinese, Japanese, Korean, or English care instructions.");
        sb.AppendLine("- Do NOT invent temperatures, watering schedules, soil recipes, or NPK values.");
        sb.AppendLine("- Decorative non-readable brush marks / sticker shapes are OK.");
        if (!string.IsNullOrWhiteSpace(scientific))
            sb.AppendLine($"- Scientific name reference only: {scientific} (do not rely on readable text rendering).");
        sb.AppendLine();
        sb.AppendLine("COMPOSITION (PlantHeroPreset):");
        sb.AppendLine($"- Preset: {PlantVisualCompositionService.GoldenReferencePresetVersion}");
        sb.AppendLine($"- Style: {composition.Style}; Layout: {composition.Layout}; Palette: {composition.ColorPalette};");
        sb.AppendLine($"- Decorations: {composition.Decoration}; doodles: {composition.DoodlePosition}; paper: {composition.PaperTexture}; brush: {composition.BrushStyle};");
        sb.AppendLine($"- Micro glitch only at: {composition.GlitchPosition}; crop feel: {composition.Crop}; wall aspect: {composition.WallAspect}.");
        sb.AppendLine();
        sb.AppendLine("OUTPUT: one landscape 16:9 botanical scrapbook base with rounded corners, cream margins, plant-dominant composition, optional small doodles/Polaroid/art labels, NO left knowledge sticky zone, and no readable text.");

        return sb.ToString();
    }
}

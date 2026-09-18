using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Services.AI;
using Midnight.EC.Plant.WEB.Services.External;
using Midnight.EC.Plant.WEB.Services.PlantProfile;

namespace Midnight.EC.Plant.WEB.Services.PlantKnowledge;

public class PlantKnowledgeService
{
    private readonly PlantSpeciesRespo _speciesRepository;
    private readonly PlantKnowledgeRespo _knowledgeRepository;
    private readonly PlantRespo _plantRepository;
    private readonly PlantProfileRespo _profileRepository;
    private readonly CareKnowledgeSynthesisService _careSynthesisService;
    private readonly EnvironmentFitService _environmentFitService;
    private readonly PlantProfileService _profileService;
    private readonly ILogger<PlantKnowledgeService> _logger;

    public PlantKnowledgeService(
        PlantSpeciesRespo speciesRepository,
        PlantKnowledgeRespo knowledgeRepository,
        PlantRespo plantRepository,
        PlantProfileRespo profileRepository,
        CareKnowledgeSynthesisService careSynthesisService,
        EnvironmentFitService environmentFitService,
        PlantProfileService profileService,
        ILogger<PlantKnowledgeService> logger)
    {
        _speciesRepository = speciesRepository;
        _knowledgeRepository = knowledgeRepository;
        _plantRepository = plantRepository;
        _profileRepository = profileRepository;
        _careSynthesisService = careSynthesisService;
        _environmentFitService = environmentFitService;
        _profileService = profileService;
        _logger = logger;
    }

    public async Task<Midnight.EC.Plant.WEB.Models.DTOs.PlantKnowledgeDto?> GetBySpeciesIdAsync(Guid speciesId, CancellationToken cancellationToken = default)
    {
        var knowledge = await _knowledgeRepository.GetBySpeciesIdAsync(speciesId, cancellationToken);
        return knowledge?.ToDto();
    }

    public async Task<EnvironmentFitResult> RefreshEnvironmentAdviceAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdWithDetailsAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var knowledgeEntity = plant.Species.Knowledge
            ?? await _knowledgeRepository.GetBySpeciesIdAsync(plant.SpeciesID, cancellationToken);
        if (knowledgeEntity == null)
        {
            return EnvironmentFitResult.Failed("尚無物種照護知識，請先重新產生照護知識。");
        }

        var profile = await _profileRepository.GetByPlantIdAsync(plantId, cancellationToken);
        if (profile == null ||
            (profile.ActualPlacement == null && profile.ActualLight == null &&
             profile.HasRainCover == null && string.IsNullOrWhiteSpace(profile.SubstrateType) &&
             profile.SaucerState == null))
        {
            return EnvironmentFitResult.Skipped();
        }

        var taboos = CareConstraintExtractor.FromJson(knowledgeEntity.CareTaboosJson);
        var suggestedLight = profile.OverrideSuggestedLight
            ?? knowledgeEntity.SuggestedLight
            ?? LightLevelDisplay.TryParseFromText(knowledgeEntity.LightRequirement);

        var env = new PlantEnvironmentContext
        {
            PlacementLabel = profile.ActualPlacement.HasValue
                ? PlacementTypeDisplay.ToLabel(profile.ActualPlacement)
                : null,
            LightLabel = profile.ActualLight.HasValue
                ? LightLevelDisplay.ToLabel(profile.ActualLight)
                : null,
            RainCoverLabel = profile.HasRainCover switch
            {
                true => "有遮雨",
                false => "無遮雨",
                _ => null
            },
            SubstrateType = profile.SubstrateType,
            SaucerLabel = profile.SaucerState.HasValue
                ? SaucerStateDisplay.ToLabel(profile.SaucerState)
                : null,
            City = profile.City,
            MismatchWarnings = CareConstraintExtractor.BuildMismatchWarnings(
                suggestedLight,
                profile.ActualLight,
                taboos,
                profile.HasRainCover,
                profile.ActualPlacement,
                profile.SubstrateType)
        };

        var knowledge = new ExternalKnowledgeResult
        {
            LightRequirement = knowledgeEntity.LightRequirement,
            WaterRequirement = knowledgeEntity.WaterRequirement,
            HumidityRequirement = knowledgeEntity.HumidityRequirement,
            TemperatureMin = knowledgeEntity.TemperatureMin,
            TemperatureMax = knowledgeEntity.TemperatureMax,
            SoilRequirement = knowledgeEntity.SoilRequirement,
            FertilizerRequirement = knowledgeEntity.FertilizerRequirement,
            GrowthSeason = knowledgeEntity.GrowthSeason,
            CareSummary = knowledgeEntity.CareSummary,
            ExternalCareGuide = knowledgeEntity.ExternalCareGuide
        };

        var displayName = plant.NickName ?? plant.Name;
        var fit = await _environmentFitService.RefreshAsync(
            displayName,
            plant.Species.ScientificName,
            knowledge,
            env,
            cancellationToken);

        if (fit.Succeeded && !string.IsNullOrWhiteSpace(fit.Advice))
        {
            await _profileService.SetAiEnvironmentAdviceAsync(plantId, fit.Advice, cancellationToken);
        }

        return fit;
    }

    public Task<KnowledgeRefreshResult> RefreshAsync(Guid speciesId, string speciesKeyword, CancellationToken cancellationToken = default) =>
        RefreshCoreAsync(speciesId, speciesKeyword, cancellationToken);

    public Task<KnowledgeRefreshResult> RefreshFromExternalAsync(Guid speciesId, string speciesKeyword, CancellationToken cancellationToken = default) =>
        RefreshAsync(speciesId, speciesKeyword, cancellationToken);

    private async Task<KnowledgeRefreshResult> RefreshCoreAsync(
        Guid speciesId,
        string speciesKeyword,
        CancellationToken cancellationToken)
    {
        var keyword = speciesKeyword?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new InvalidOperationException("請提供植物名稱。");
        }

        var species = await _speciesRepository.GetByIdAsync(speciesId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物物種。");

        var scientificName = !string.IsNullOrWhiteSpace(species.ScientificName)
            && !string.Equals(species.ScientificName, "Pending", StringComparison.OrdinalIgnoreCase)
            && !species.ScientificName.StartsWith("Pending-", StringComparison.OrdinalIgnoreCase)
            ? species.ScientificName.Trim()
            : keyword;

        var trustedChinese = CareGuideJson.TrustedChineseName(
            keyword,
            species.ChineseName);

        var synthesis = await _careSynthesisService.SynthesizeAsync(
            trustedChinese ?? keyword,
            scientificName,
            cancellationToken);

        if (synthesis.Status == CareSynthesisAttemptStatus.ServiceFailed || synthesis.Partial == null)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(synthesis.FailureReason)
                    ? "OpenAI 產生照護知識失敗。"
                    : synthesis.FailureReason);
        }

        var externalKnowledge = new ExternalKnowledgeResult { Provider = "none" };
        var aiCareGuide = synthesis.Partial.ExternalCareGuide;

        if (!string.IsNullOrWhiteSpace(trustedChinese))
        {
            await EnrichSpeciesMetadataAsync(species, new ExternalSpeciesResult
            {
                ScientificName = scientificName,
                ChineseName = trustedChinese,
                Genus = species.Genus,
                Family = species.Family
            }, cancellationToken);
        }

        var displayName = trustedChinese
            ?? species.ChineseName
            ?? species.CommonName
            ?? keyword;

        var existing = await _knowledgeRepository.GetBySpeciesIdAsync(speciesId, cancellationToken);
        var now = DateTime.UtcNow;

        Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel saved;
        if (existing == null)
        {
            var knowledge = MapNewKnowledge(speciesId, externalKnowledge, now);
            ApplyExternalCareGuide(
                knowledge,
                displayName,
                scientificName,
                trustedChinese,
                aiCareGuide);
            ApplyStructuredConstraints(knowledge);
            // 強制覆蓋：Merge 後再寫入 AI 全量
            OverwriteFromPartial(knowledge, synthesis.Partial);

            await _knowledgeRepository.InsertAsync(knowledge, cancellationToken);
            _logger.LogInformation("AI-wrote plant knowledge for species {SpeciesId}", speciesId);
            saved = knowledge;
        }
        else
        {
            OverwriteFromPartial(existing, synthesis.Partial);
            ApplyExternalCareGuide(
                existing,
                displayName,
                scientificName,
                trustedChinese,
                aiCareGuide);
            ApplyStructuredConstraints(existing);

            existing.DataVersion += 1;
            existing.SourceUpdatedAt = now;
            existing.ModifyDate = now;
            await _knowledgeRepository.UpdateAsync(existing, cancellationToken);
            _logger.LogInformation("AI-refreshed plant knowledge for species {SpeciesId}", speciesId);
            saved = existing;
        }

        return new KnowledgeRefreshResult
        {
            Knowledge = saved.ToDto(),
            AiSupplement = ResolveAiSupplementOutcome(synthesis.Status, saved),
            AiFailureReason = synthesis.FailureReason
        };
    }

    private static void OverwriteFromPartial(
        Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel knowledge,
        ExternalKnowledgePartial partial)
    {
        if (!string.IsNullOrWhiteSpace(partial.LightRequirement))
            knowledge.LightRequirement = partial.LightRequirement;
        if (!string.IsNullOrWhiteSpace(partial.WaterRequirement))
            knowledge.WaterRequirement = partial.WaterRequirement;
        if (!string.IsNullOrWhiteSpace(partial.HumidityRequirement))
            knowledge.HumidityRequirement = partial.HumidityRequirement;
        if (partial.TemperatureMin.HasValue)
            knowledge.TemperatureMin = partial.TemperatureMin;
        if (partial.TemperatureMax.HasValue)
            knowledge.TemperatureMax = partial.TemperatureMax;
        if (!string.IsNullOrWhiteSpace(partial.SoilRequirement))
            knowledge.SoilRequirement = partial.SoilRequirement;
        if (!string.IsNullOrWhiteSpace(partial.FertilizerRequirement))
            knowledge.FertilizerRequirement = partial.FertilizerRequirement;
        if (!string.IsNullOrWhiteSpace(partial.GrowthSeason))
            knowledge.GrowthSeason = partial.GrowthSeason;
        if (!string.IsNullOrWhiteSpace(partial.CareSummary))
            knowledge.CareSummary = partial.CareSummary;
        if (partial.SuggestedLight.HasValue)
            knowledge.SuggestedLight = partial.SuggestedLight;
        if (!string.IsNullOrWhiteSpace(partial.ExternalCareGuide))
            knowledge.ExternalCareGuide = partial.ExternalCareGuide;
    }

    private static AiSupplementOutcome ResolveAiSupplementOutcome(
        CareSynthesisAttemptStatus status,
        Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel knowledge) =>
        status switch
        {
            CareSynthesisAttemptStatus.SkippedAlreadyComplete => AiSupplementOutcome.NotNeeded,
            CareSynthesisAttemptStatus.ServiceFailed => AiSupplementOutcome.ServiceFailed,
            CareSynthesisAttemptStatus.Succeeded => CareKnowledgeCompleteness.HasGaps(knowledge)
                ? AiSupplementOutcome.AppliedWithRemainingGaps
                : AiSupplementOutcome.Applied,
            _ => AiSupplementOutcome.NotNeeded
        };

    private static void ApplyExternalCareGuide(
        Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel knowledge,
        string displayName,
        string? scientificName,
        string? trustedChineseName,
        string? aiCareGuide)
    {
        if (!string.IsNullOrWhiteSpace(aiCareGuide))
        {
            var guide = CareGuideJson.TryParseSpeciesGuide(aiCareGuide);
            if (guide != null)
            {
                guide.ScientificName = !string.IsNullOrWhiteSpace(scientificName)
                    ? scientificName.Trim()
                    : guide.ScientificName;
                guide.ChineseName = trustedChineseName;

                var idMod = guide.FindModule(CareGuideModuleIds.Identification);
                if (idMod != null && !string.IsNullOrWhiteSpace(idMod.Content) && !string.IsNullOrWhiteSpace(guide.ScientificName))
                {
                    idMod.Content = RewriteSummaryLead(idMod.Content, guide.ScientificName, trustedChineseName);
                }

                var stored = CareGuideJson.ForStorage(guide);
                knowledge.ExternalCareGuide = CareGuideJson.Serialize(stored);
                CareGuideJson.ProjectQuickFactsToKnowledge(stored.QuickFacts, knowledge);
                CareGuideJson.ProjectSoilModuleToKnowledge(stored, knowledge);

                var fertText = CareGuideJson.FormatFertilizerRequirement(stored.FertilizerRecipe);
                if (!string.IsNullOrWhiteSpace(fertText))
                {
                    knowledge.FertilizerRequirement = fertText;
                }

                var intro = stored.GetWallIntro();
                if (!string.IsNullOrWhiteSpace(intro))
                {
                    knowledge.CareSummary = intro.Length > 400 ? intro[..400] + "…" : intro;
                }

                return;
            }

            knowledge.ExternalCareGuide = ExternalCareGuideBuilder.SanitizeForDisplay(aiCareGuide);
            return;
        }

        var current = ExternalCareGuideBuilder.SanitizeForDisplay(knowledge.ExternalCareGuide);
        if (!string.IsNullOrWhiteSpace(current) && CareGuideJson.LooksLikeJson(current))
        {
            var existingGuide = CareGuideJson.TryParseSpeciesGuide(current);
            if (existingGuide != null)
            {
                existingGuide.ScientificName = !string.IsNullOrWhiteSpace(scientificName)
                    ? scientificName.Trim()
                    : existingGuide.ScientificName;
                existingGuide.ChineseName = trustedChineseName;
                var stored = CareGuideJson.ForStorage(existingGuide);
                knowledge.ExternalCareGuide = CareGuideJson.Serialize(stored);
                CareGuideJson.ProjectQuickFactsToKnowledge(stored.QuickFacts, knowledge);
                CareGuideJson.ProjectSoilModuleToKnowledge(stored, knowledge);
                return;
            }

            knowledge.ExternalCareGuide = current;
            return;
        }

        if (string.IsNullOrWhiteSpace(current))
        {
            current = ExternalCareGuideBuilder.SanitizeForDisplay(BuildExternalCareGuide(displayName, knowledge));
        }

        knowledge.ExternalCareGuide = current;
    }

    private static string RewriteSummaryLead(string summary, string scientificName, string? trustedChineseName)
    {
        var trimmed = summary.Trim();
        // 若摘要以「某某是／為」開頭且不是學名／信任中文名，改寫開頭
        var leadMatch = System.Text.RegularExpressions.Regex.Match(
            trimmed,
            @"^([\u4e00-\u9fffA-Za-z0-9·．.\s]{1,20}?)(是一種|是一|為一種|為一|是)");
        if (!leadMatch.Success)
        {
            return trimmed;
        }

        var leadName = leadMatch.Groups[1].Value.Trim();
        var ok = string.Equals(leadName, scientificName, StringComparison.OrdinalIgnoreCase)
                 || (!string.IsNullOrWhiteSpace(trustedChineseName)
                     && string.Equals(leadName, trustedChineseName, StringComparison.Ordinal));
        if (ok)
        {
            return trimmed;
        }

        var rest = trimmed[leadMatch.Length..].TrimStart();
        var label = !string.IsNullOrWhiteSpace(trustedChineseName)
            ? $"{scientificName}（{trustedChineseName}）"
            : scientificName;
        return $"{label}{leadMatch.Groups[2].Value}{rest}";
    }

    private async Task EnrichSpeciesMetadataAsync(PlantSpeciesModel species, ExternalSpeciesResult? externalSpecies, CancellationToken cancellationToken)
    {
        if (externalSpecies == null)
        {
            return;
        }

        var changed = false;
        if (string.IsNullOrWhiteSpace(species.Genus) && !string.IsNullOrWhiteSpace(externalSpecies.Genus))
        {
            species.Genus = externalSpecies.Genus;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(species.Family) && !string.IsNullOrWhiteSpace(externalSpecies.Family))
        {
            species.Family = externalSpecies.Family;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(species.ChineseName) && !string.IsNullOrWhiteSpace(externalSpecies.ChineseName))
        {
            species.ChineseName = externalSpecies.ChineseName;
            changed = true;
        }

        if (changed)
        {
            species.ModifyDate = DateTime.UtcNow;
            await _speciesRepository.UpdateAsync(species, cancellationToken);
        }
    }

    private static Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel MapNewKnowledge(Guid speciesId, ExternalKnowledgeResult external, DateTime now)
    {
        var knowledge = new Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel
        {
            SpeciesId = speciesId,
            LightRequirement = external.LightRequirement,
            WaterRequirement = external.WaterRequirement,
            HumidityRequirement = external.HumidityRequirement,
            TemperatureMin = external.TemperatureMin,
            TemperatureMax = external.TemperatureMax,
            SoilRequirement = external.SoilRequirement,
            FertilizerRequirement = external.FertilizerRequirement,
            GrowthSeason = external.GrowthSeason,
            CareSummary = external.CareSummary,
            ExternalCareGuide = external.ExternalCareGuide,
            SuggestedLight = external.SuggestedLight,
            SourceUpdatedAt = now,
            DataVersion = 1,
            UpdatedAt = now
        };
        ApplyStructuredConstraints(knowledge);
        return knowledge;
    }

    private static void ApplyStructuredConstraints(Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel knowledge)
    {
        knowledge.SuggestedLight = knowledge.SuggestedLight
            ?? LightLevelDisplay.TryParseFromText(knowledge.LightRequirement)
            ?? LightLevelDisplay.TryParseFromText(knowledge.CareSummary)
            ?? LightLevelDisplay.TryParseFromText(knowledge.ExternalCareGuide);

        var taboos = CareConstraintExtractor.ExtractTaboos(
            knowledge.LightRequirement,
            knowledge.WaterRequirement,
            knowledge.SoilRequirement,
            knowledge.CareSummary,
            knowledge.ExternalCareGuide,
            knowledge.CommonProblems);
        knowledge.CareTaboosJson = CareConstraintExtractor.ToJson(taboos);
        knowledge.SuggestedWateringIntervalDays = CareConstraintExtractor.InferWateringIntervalDays(knowledge.WaterRequirement);
    }

    private static void MergePartial(Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel target, ExternalKnowledgePartial partial)
    {
        target.LightRequirement = Coalesce(target.LightRequirement, partial.LightRequirement);
        target.WaterRequirement = Coalesce(target.WaterRequirement, partial.WaterRequirement);
        target.HumidityRequirement = Coalesce(target.HumidityRequirement, partial.HumidityRequirement);
        target.SoilRequirement = Coalesce(target.SoilRequirement, partial.SoilRequirement);
        target.FertilizerRequirement = Coalesce(target.FertilizerRequirement, partial.FertilizerRequirement);
        target.GrowthSeason = Coalesce(target.GrowthSeason, partial.GrowthSeason);
        target.TemperatureMin ??= partial.TemperatureMin;
        target.TemperatureMax ??= partial.TemperatureMax;
        target.SuggestedLight ??= partial.SuggestedLight;

        if (!string.IsNullOrWhiteSpace(partial.CareSummary))
        {
            target.CareSummary = string.IsNullOrWhiteSpace(target.CareSummary)
                ? partial.CareSummary
                : target.CareSummary.Contains(partial.CareSummary, StringComparison.Ordinal)
                    ? target.CareSummary
                    : $"{target.CareSummary} {partial.CareSummary}";
        }
    }

    private static void MergeKnowledge(Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel target, ExternalKnowledgeResult external)
    {
        target.LightRequirement = Coalesce(target.LightRequirement, external.LightRequirement);
        target.WaterRequirement = Coalesce(target.WaterRequirement, external.WaterRequirement);
        target.HumidityRequirement = Coalesce(target.HumidityRequirement, external.HumidityRequirement);
        target.SoilRequirement = Coalesce(target.SoilRequirement, external.SoilRequirement);
        target.FertilizerRequirement = Coalesce(target.FertilizerRequirement, external.FertilizerRequirement);
        target.GrowthSeason = Coalesce(target.GrowthSeason, external.GrowthSeason);
        target.TemperatureMin ??= external.TemperatureMin;
        target.TemperatureMax ??= external.TemperatureMax;
        // 同步重產：AI／外部給的建議日照可覆寫舊值（解析失敗殘留）
        if (external.SuggestedLight.HasValue)
        {
            target.SuggestedLight = external.SuggestedLight;
        }

        if (!string.IsNullOrWhiteSpace(external.CareSummary))
        {
            target.CareSummary = string.IsNullOrWhiteSpace(target.CareSummary)
                ? external.CareSummary
                : external.CareSummary.Contains(target.CareSummary, StringComparison.Ordinal)
                    ? external.CareSummary
                    : $"{external.CareSummary} {target.CareSummary}";
        }

        if (!string.IsNullOrWhiteSpace(external.ExternalCareGuide))
        {
            target.ExternalCareGuide = external.ExternalCareGuide;
        }
    }

    private static string? BuildExternalCareGuide(string displayName, Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel knowledge) =>
        ExternalCareGuideBuilder.Build(displayName, new ExternalKnowledgeResult
        {
            LightRequirement = knowledge.LightRequirement,
            WaterRequirement = knowledge.WaterRequirement,
            HumidityRequirement = knowledge.HumidityRequirement,
            TemperatureMin = knowledge.TemperatureMin,
            TemperatureMax = knowledge.TemperatureMax,
            SoilRequirement = knowledge.SoilRequirement,
            FertilizerRequirement = knowledge.FertilizerRequirement,
            GrowthSeason = knowledge.GrowthSeason
        });

    private static string? Coalesce(string? current, string? incoming) =>
        string.IsNullOrWhiteSpace(current) ? incoming : current;
}

using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Services.AI;
using Midnight.EC.Plant.WEB.Services.External;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Services.PlantProfile;

namespace Midnight.EC.Plant.WEB.Services.PlantKnowledge;

public class PlantKnowledgeService
{
    private readonly PlantSpeciesRespo _speciesRepository;
    private readonly PlantKnowledgeRespo _knowledgeRepository;
    private readonly PlantRespo _plantRepository;
    private readonly PlantProfileRespo _profileRepository;
    private readonly IExternalPlantApiService _externalPlantApiService;
    private readonly ICareKnowledgeSynthesisService _careSynthesisService;
    private readonly PlantProfileService _profileService;
    private readonly ILogger<PlantKnowledgeService> _logger;

    public PlantKnowledgeService(
        PlantSpeciesRespo speciesRepository,
        PlantKnowledgeRespo knowledgeRepository,
        PlantRespo plantRepository,
        PlantProfileRespo profileRepository,
        IExternalPlantApiService externalPlantApiService,
        ICareKnowledgeSynthesisService careSynthesisService,
        PlantProfileService profileService,
        ILogger<PlantKnowledgeService> logger)
    {
        _speciesRepository = speciesRepository;
        _knowledgeRepository = knowledgeRepository;
        _plantRepository = plantRepository;
        _profileRepository = profileRepository;
        _externalPlantApiService = externalPlantApiService;
        _careSynthesisService = careSynthesisService;
        _profileService = profileService;
        _logger = logger;
    }

    public async Task<Midnight.EC.Plant.WEB.Models.DTOs.PlantKnowledgeDto?> GetBySpeciesIdAsync(Guid speciesId, CancellationToken cancellationToken = default)
    {
        var knowledge = await _knowledgeRepository.GetBySpeciesIdAsync(speciesId, cancellationToken);
        return knowledge?.ToDto();
    }

    public async Task<Midnight.EC.Plant.WEB.Models.DTOs.PlantKnowledgeDto> SyncFromExternalAsync(Guid speciesId, string speciesKeyword, CancellationToken cancellationToken = default)
    {
        var existing = await _knowledgeRepository.GetBySpeciesIdAsync(speciesId, cancellationToken);
        if (existing != null)
        {
            return existing.ToDto();
        }

        var refreshed = await RefreshFromExternalAsync(speciesId, speciesKeyword, cancellationToken);
        return refreshed.Knowledge;
    }

    public async Task<EnvironmentFitResult> RefreshEnvironmentAdviceAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdWithDetailsAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var knowledgeEntity = plant.Species.Knowledge
            ?? await _knowledgeRepository.GetBySpeciesIdAsync(plant.SpeciesID, cancellationToken);
        if (knowledgeEntity == null)
        {
            return EnvironmentFitResult.Failed("尚無物種照護知識，請先同步外部來源。");
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
        var fit = await _careSynthesisService.SynthesizeEnvironmentFitAsync(
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

    public Task<KnowledgeRefreshResult> RefreshFromExternalAsync(Guid speciesId, string speciesKeyword, CancellationToken cancellationToken = default) =>
        RefreshFromExternalAsync(speciesId, speciesKeyword, null, null, cancellationToken);

    public async Task<KnowledgeRefreshResult> RefreshFromExternalAsync(
        Guid speciesId,
        string? speciesKeyword,
        Stream? identificationImage,
        string? imageFileName,
        CancellationToken cancellationToken = default)
    {
        var keyword = speciesKeyword?.Trim() ?? string.Empty;
        if (identificationImage != null)
        {
            var identified = await _externalPlantApiService.IdentifyFromImageAsync(
                identificationImage,
                imageFileName ?? "plant.jpg",
                cancellationToken);
            if (identified != null)
            {
                keyword = identified.ScientificName;
                _logger.LogInformation(
                    "Identified plant as {ScientificName} (confidence {Confidence:P0})",
                    identified.ScientificName,
                    identified.Confidence);
            }
            else if (string.IsNullOrWhiteSpace(keyword))
            {
                throw new InvalidOperationException("無法從照片辨識植物，請改輸入名稱或上傳更清晰的照片。");
            }
        }

        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new InvalidOperationException("請提供植物名稱或上傳照片。");
        }

        return await RefreshFromExternalCoreAsync(speciesId, keyword, speciesKeyword?.Trim(), cancellationToken);
    }

    private async Task<KnowledgeRefreshResult> RefreshFromExternalCoreAsync(
        Guid speciesId,
        string searchKeyword,
        string? displayKeyword,
        CancellationToken cancellationToken)
    {
        var species = await _speciesRepository.GetByIdAsync(speciesId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物物種。");

        var external = await _externalPlantApiService.SearchSpeciesAsync(searchKeyword, cancellationToken)
            ?? throw new InvalidOperationException("無法從外部 API 取得植物知識，請嘗試調整搜尋關鍵字。");

        if (external.Knowledge == null)
        {
            throw new InvalidOperationException("外部 API 未回傳可用知識資料。");
        }

        var synthesis = await _careSynthesisService.SynthesizeAsync(
            displayKeyword ?? searchKeyword,
            external.Species?.ScientificName ?? species.ScientificName,
            external.Knowledge,
            cancellationToken,
            forceRefreshGuide: true);
        var aiCareGuide = synthesis.Partial?.ExternalCareGuide;
        if (synthesis.Partial != null)
        {
            external.Knowledge = MergeExternalWithPartial(external.Knowledge, synthesis.Partial);
            aiCareGuide ??= external.Knowledge.ExternalCareGuide;
        }

        if (external.Species != null && !string.IsNullOrWhiteSpace(displayKeyword) &&
            displayKeyword.Any(c => c >= 0x4E00 && c <= 0x9FFF))
        {
            external.Species.ChineseName ??= displayKeyword;
        }

        await EnrichSpeciesMetadataAsync(species, external.Species, cancellationToken);

        var displayName = displayKeyword
            ?? species.ChineseName
            ?? species.CommonName
            ?? searchKeyword;

        var existing = await _knowledgeRepository.GetBySpeciesIdAsync(speciesId, cancellationToken);
        var now = DateTime.UtcNow;
        var careTemplate = GenusCareTemplateProvider.TryGetFromKeyword(displayKeyword ?? searchKeyword)
            ?? GenusCareTemplateProvider.TryGet(species.Genus, species.Family);

        Midnight.EC.Plant.WEB.Models.Models.PlantKnowledgeModel saved;
        if (existing == null)
        {
            var knowledge = MapNewKnowledge(speciesId, external.Knowledge, now);
            if (careTemplate != null)
            {
                MergePartial(knowledge, careTemplate);
            }

            ApplyExternalCareGuide(
                knowledge,
                displayName,
                external.Species?.ScientificName ?? species.ScientificName,
                CareGuideJson.TrustedChineseName(displayKeyword, species.ChineseName ?? external.Species?.ChineseName),
                aiCareGuide);
            ApplyStructuredConstraints(knowledge);

            await _knowledgeRepository.InsertAsync(knowledge, cancellationToken);
            _logger.LogInformation("Synced plant knowledge for species {SpeciesId} from {Provider}", speciesId, external.Knowledge.Provider);
            saved = knowledge;
        }
        else
        {
            MergeKnowledge(existing, external.Knowledge);

            if (careTemplate != null)
            {
                MergePartial(existing, careTemplate);
            }

            ApplyExternalCareGuide(
                existing,
                displayName,
                external.Species?.ScientificName ?? species.ScientificName,
                CareGuideJson.TrustedChineseName(displayKeyword, species.ChineseName ?? external.Species?.ChineseName),
                aiCareGuide);
            ApplyStructuredConstraints(existing);

            existing.DataVersion += 1;
            existing.SourceUpdatedAt = now;
            existing.ModifyDate = now;
            await _knowledgeRepository.UpdateAsync(existing, cancellationToken);
            _logger.LogInformation("Refreshed plant knowledge for species {SpeciesId} from {Provider}", speciesId, external.Knowledge.Provider);
            saved = existing;
        }

        return new KnowledgeRefreshResult
        {
            Knowledge = saved.ToDto(),
            AiSupplement = ResolveAiSupplementOutcome(synthesis.Status, saved),
            AiFailureReason = synthesis.FailureReason
        };
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
        // 同步時若 AI 產出結構化說明，一律覆蓋（舊長文／舊 JSON 都重產）
        if (!string.IsNullOrWhiteSpace(aiCareGuide))
        {
            var guide = CareGuideJson.TryParseSpeciesGuide(aiCareGuide);
            if (guide != null)
            {
                // 學名以外部／物種為準；中文名只信任使用者或資料庫，丟棄 AI 自造俗名
                guide.ScientificName = !string.IsNullOrWhiteSpace(scientificName)
                    ? scientificName.Trim()
                    : guide.ScientificName;
                guide.ChineseName = trustedChineseName;

                // 摘要若以錯誤中文名開頭，改以學名起述（避免「珍珠樹是…」）
                if (!string.IsNullOrWhiteSpace(guide.Summary) && !string.IsNullOrWhiteSpace(guide.ScientificName))
                {
                    guide.Summary = RewriteSummaryLead(guide.Summary, guide.ScientificName, trustedChineseName);
                }

                knowledge.ExternalCareGuide = CareGuideJson.Serialize(guide);
                var fertText = CareGuideJson.FormatFertilizerRequirement(guide.Fertilizer);
                if (!string.IsNullOrWhiteSpace(fertText))
                {
                    knowledge.FertilizerRequirement = fertText;
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
                if (!string.IsNullOrWhiteSpace(existingGuide.Summary) && !string.IsNullOrWhiteSpace(existingGuide.ScientificName))
                {
                    existingGuide.Summary = RewriteSummaryLead(
                        existingGuide.Summary,
                        existingGuide.ScientificName,
                        trustedChineseName);
                }

                knowledge.ExternalCareGuide = CareGuideJson.Serialize(existingGuide);
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

    private static ExternalKnowledgeResult MergeExternalWithPartial(ExternalKnowledgeResult target, ExternalKnowledgePartial partial)
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
                : $"{target.CareSummary} {partial.CareSummary}";
        }

        if (!string.IsNullOrWhiteSpace(partial.ExternalCareGuide))
        {
            // 同步重產：允許 AI 結構化說明覆蓋舊長文
            target.ExternalCareGuide = partial.ExternalCareGuide;
        }

        if (!string.IsNullOrWhiteSpace(partial.Provider))
        {
            target.Provider = string.IsNullOrWhiteSpace(target.Provider)
                ? partial.Provider
                : $"{target.Provider}+{partial.Provider}";
        }

        return target;
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

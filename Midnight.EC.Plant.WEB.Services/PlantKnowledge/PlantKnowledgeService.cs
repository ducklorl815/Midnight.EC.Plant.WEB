using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.AI;
using Midnight.EC.Plant.WEB.Services.External;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.PlantKnowledge;

public class PlantKnowledgeService : IPlantKnowledgeService
{
    private readonly IPlantSpeciesRepository _speciesRepository;
    private readonly IPlantKnowledgeRepository _knowledgeRepository;
    private readonly IExternalPlantApiService _externalPlantApiService;
    private readonly ICareKnowledgeSynthesisService _careSynthesisService;
    private readonly ILogger<PlantKnowledgeService> _logger;

    public PlantKnowledgeService(
        IPlantSpeciesRepository speciesRepository,
        IPlantKnowledgeRepository knowledgeRepository,
        IExternalPlantApiService externalPlantApiService,
        ICareKnowledgeSynthesisService careSynthesisService,
        ILogger<PlantKnowledgeService> logger)
    {
        _speciesRepository = speciesRepository;
        _knowledgeRepository = knowledgeRepository;
        _externalPlantApiService = externalPlantApiService;
        _careSynthesisService = careSynthesisService;
        _logger = logger;
    }

    public async Task<Midnight.EC.Plant.WEB.Models.DTOs.PlantKnowledgeDto?> GetBySpeciesIdAsync(int speciesId, CancellationToken cancellationToken = default)
    {
        var knowledge = await _knowledgeRepository.GetBySpeciesIdAsync(speciesId, cancellationToken);
        return knowledge?.ToDto();
    }

    public async Task<Midnight.EC.Plant.WEB.Models.DTOs.PlantKnowledgeDto> SyncFromExternalAsync(int speciesId, string speciesKeyword, CancellationToken cancellationToken = default)
    {
        var existing = await _knowledgeRepository.GetBySpeciesIdAsync(speciesId, cancellationToken);
        if (existing != null)
        {
            return existing.ToDto();
        }

        return await RefreshFromExternalAsync(speciesId, speciesKeyword, cancellationToken);
    }

    public Task<Midnight.EC.Plant.WEB.Models.DTOs.PlantKnowledgeDto> RefreshFromExternalAsync(int speciesId, string speciesKeyword, CancellationToken cancellationToken = default) =>
        RefreshFromExternalAsync(speciesId, speciesKeyword, null, null, cancellationToken);

    public async Task<Midnight.EC.Plant.WEB.Models.DTOs.PlantKnowledgeDto> RefreshFromExternalAsync(
        int speciesId,
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

    private async Task<Midnight.EC.Plant.WEB.Models.DTOs.PlantKnowledgeDto> RefreshFromExternalCoreAsync(
        int speciesId,
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

        var aiPartial = await _careSynthesisService.SynthesizeAsync(
            displayKeyword ?? searchKeyword,
            external.Species?.ScientificName ?? species.ScientificName,
            external.Knowledge,
            cancellationToken);
        var aiCareGuide = aiPartial?.ExternalCareGuide;
        if (aiPartial != null)
        {
            external.Knowledge = MergeExternalWithPartial(external.Knowledge, aiPartial);
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

        if (existing == null)
        {
            var knowledge = MapNewKnowledge(speciesId, external.Knowledge, now);
            if (careTemplate != null)
            {
                MergePartial(knowledge, careTemplate);
            }

            knowledge.ExternalCareGuide = ExternalCareGuideBuilder.SanitizeForDisplay(
                !string.IsNullOrWhiteSpace(aiCareGuide)
                    ? aiCareGuide
                    : BuildExternalCareGuide(displayName, knowledge));
            ApplyStructuredConstraints(knowledge);

            await _knowledgeRepository.AddAsync(knowledge, cancellationToken);
            await _knowledgeRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Synced plant knowledge for species {SpeciesId} from {Provider}", speciesId, external.Knowledge.Provider);
            return knowledge.ToDto();
        }

        MergeKnowledge(existing, external.Knowledge);

        if (careTemplate != null)
        {
            MergePartial(existing, careTemplate);
        }

        existing.ExternalCareGuide = ExternalCareGuideBuilder.SanitizeForDisplay(
            !string.IsNullOrWhiteSpace(aiCareGuide)
                ? aiCareGuide
                : BuildExternalCareGuide(displayName, existing));
        ApplyStructuredConstraints(existing);

        existing.DataVersion += 1;
        existing.SourceUpdatedAt = now;
        existing.UpdatedAt = now;
        await _knowledgeRepository.UpdateAsync(existing, cancellationToken);
        await _knowledgeRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Refreshed plant knowledge for species {SpeciesId} from {Provider}", speciesId, external.Knowledge.Provider);
        return existing.ToDto();
    }

    private async Task EnrichSpeciesMetadataAsync(PlantSpecies species, ExternalSpeciesResult? externalSpecies, CancellationToken cancellationToken)
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
            species.UpdatedAt = DateTime.UtcNow;
            await _speciesRepository.UpdateAsync(species, cancellationToken);
            await _speciesRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private static Midnight.EC.Plant.WEB.Models.Entities.PlantKnowledge MapNewKnowledge(int speciesId, ExternalKnowledgeResult external, DateTime now)
    {
        var knowledge = new Midnight.EC.Plant.WEB.Models.Entities.PlantKnowledge
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
            SourceUpdatedAt = now,
            DataVersion = 1,
            UpdatedAt = now
        };
        ApplyStructuredConstraints(knowledge);
        return knowledge;
    }

    private static void ApplyStructuredConstraints(Midnight.EC.Plant.WEB.Models.Entities.PlantKnowledge knowledge)
    {
        knowledge.SuggestedLight = LightLevelDisplay.TryParseFromText(knowledge.LightRequirement)
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

    private static void MergePartial(Midnight.EC.Plant.WEB.Models.Entities.PlantKnowledge target, ExternalKnowledgePartial partial)
    {
        target.LightRequirement = Coalesce(target.LightRequirement, partial.LightRequirement);
        target.WaterRequirement = Coalesce(target.WaterRequirement, partial.WaterRequirement);
        target.HumidityRequirement = Coalesce(target.HumidityRequirement, partial.HumidityRequirement);
        target.SoilRequirement = Coalesce(target.SoilRequirement, partial.SoilRequirement);
        target.FertilizerRequirement = Coalesce(target.FertilizerRequirement, partial.FertilizerRequirement);
        target.GrowthSeason = Coalesce(target.GrowthSeason, partial.GrowthSeason);
        target.TemperatureMin ??= partial.TemperatureMin;
        target.TemperatureMax ??= partial.TemperatureMax;

        if (!string.IsNullOrWhiteSpace(partial.CareSummary))
        {
            target.CareSummary = string.IsNullOrWhiteSpace(target.CareSummary)
                ? partial.CareSummary
                : target.CareSummary.Contains(partial.CareSummary, StringComparison.Ordinal)
                    ? target.CareSummary
                    : $"{target.CareSummary} {partial.CareSummary}";
        }
    }

    private static void MergeKnowledge(Midnight.EC.Plant.WEB.Models.Entities.PlantKnowledge target, ExternalKnowledgeResult external)
    {
        target.LightRequirement = Coalesce(target.LightRequirement, external.LightRequirement);
        target.WaterRequirement = Coalesce(target.WaterRequirement, external.WaterRequirement);
        target.HumidityRequirement = Coalesce(target.HumidityRequirement, external.HumidityRequirement);
        target.SoilRequirement = Coalesce(target.SoilRequirement, external.SoilRequirement);
        target.FertilizerRequirement = Coalesce(target.FertilizerRequirement, external.FertilizerRequirement);
        target.GrowthSeason = Coalesce(target.GrowthSeason, external.GrowthSeason);
        target.TemperatureMin ??= external.TemperatureMin;
        target.TemperatureMax ??= external.TemperatureMax;

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
        if (!string.IsNullOrWhiteSpace(partial.CareSummary))
        {
            target.CareSummary = string.IsNullOrWhiteSpace(target.CareSummary)
                ? partial.CareSummary
                : $"{target.CareSummary} {partial.CareSummary}";
        }

        if (!string.IsNullOrWhiteSpace(partial.ExternalCareGuide))
        {
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

    private static string? BuildExternalCareGuide(string displayName, Midnight.EC.Plant.WEB.Models.Entities.PlantKnowledge knowledge) =>
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

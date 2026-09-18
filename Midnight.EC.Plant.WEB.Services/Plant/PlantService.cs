using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.Services.PlantKnowledge;
using Midnight.EC.Plant.WEB.Services.PlantProfile;
using Midnight.EC.Plant.WEB.Services.PlantCare;

namespace Midnight.EC.Plant.WEB.Services.Plant;

public class PlantService
{
    public const int DefaultWateringIntervalDays = 7;
    public const string PendingScientificName = "未確認";

    private readonly PlantRespo _plantRepository;
    private readonly PlantSpeciesRespo _speciesRepository;
    private readonly IExternalPlantApiService _externalPlantApiService;
    private readonly PlantKnowledgeService _plantKnowledgeService;
    private readonly PlantProfileService _plantProfileService;
    private readonly PlantCareService _plantCareService;
    private readonly ILogger<PlantService> _logger;

    public PlantService(
        PlantRespo plantRepository,
        PlantSpeciesRespo speciesRepository,
        IExternalPlantApiService externalPlantApiService,
        PlantKnowledgeService plantKnowledgeService,
        PlantProfileService plantProfileService,
        PlantCareService plantCareService,
        ILogger<PlantService> logger)
    {
        _plantRepository = plantRepository;
        _speciesRepository = speciesRepository;
        _externalPlantApiService = externalPlantApiService;
        _plantKnowledgeService = plantKnowledgeService;
        _plantProfileService = plantProfileService;
        _plantCareService = plantCareService;
        _logger = logger;
    }

    public Task<List<PlantDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        GetAllInternalAsync(cancellationToken);

    public Task<PlantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdInternalAsync(id, cancellationToken);

    public Task<bool> IsNickNameTakenAsync(string nickName, Guid? excludePlantId = null, CancellationToken cancellationToken = default) =>
        _plantRepository.IsNickNameTakenAsync(nickName, excludePlantId, cancellationToken);

    public Task<IReadOnlyList<ExternalSpeciesResult>> SearchSpeciesCandidatesAsync(
        string keyword,
        CancellationToken cancellationToken = default) =>
        _externalPlantApiService.SearchSpeciesCandidatesAsync(keyword, cancellationToken);

    public Task<PlantDto> CreateAsync(
        string name,
        string? speciesKeyword,
        string? nickName,
        string? location,
        string? description,
        DateTime? startDate,
        CancellationToken cancellationToken = default) =>
        CreateAsync(name, speciesKeyword, null, null, nickName, location, description, startDate, cancellationToken);

    public async Task<PlantDto> CreateAsync(
        string name,
        string? speciesKeyword,
        Stream? identificationImage,
        string? identificationFileName,
        string? nickName,
        string? location,
        string? description,
        DateTime? startDate,
        CancellationToken cancellationToken = default)
    {
        // Legacy path: try first candidate or pending; prefer CreateFromDraftAsync from controller.
        var keyword = speciesKeyword?.Trim() ?? name.Trim();
        ExternalSpeciesResult? confirmed = null;
        try
        {
            var candidates = await SearchSpeciesCandidatesAsync(keyword, cancellationToken);
            confirmed = candidates.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Legacy create: candidate search failed, creating pending species");
        }

        return await CreateFromDraftAsync(
            new CreatePlantDraft
            {
                ChineseName = keyword,
                NickName = string.IsNullOrWhiteSpace(nickName) ? name.Trim() : nickName.Trim(),
                Location = location,
                Description = description,
                StartDate = startDate,
                WateredToday = true
            },
            confirmed,
            cancellationToken);
    }

    public async Task<Guid> EnsureSpeciesKnowledgeAsync(
        ExternalSpeciesResult confirmedSpecies,
        string chineseName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(confirmedSpecies.ScientificName))
        {
            throw new InvalidOperationException("物種學名不可空白。");
        }

        var species = await ResolveOrCreateSpeciesAsync(confirmedSpecies, chineseName.Trim(), cancellationToken);
        try
        {
            // 建檔／確保知識時一律強制同步，與詳情頁「同步外部」同格式（結構化 speciesGuide）
            await _plantKnowledgeService.RefreshFromExternalAsync(species.ID, chineseName.Trim(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Knowledge sync failed for species {SpeciesId} during ensure", species.ID);
        }

        return species.ID;
    }

    public async Task<PlantDto> CreateFromDraftAsync(
        CreatePlantDraft draft,
        ExternalSpeciesResult? confirmedSpecies,
        CancellationToken cancellationToken = default)
    {
        var chineseName = draft.ChineseName.Trim();
        var nickName = draft.NickName.Trim();
        if (string.IsNullOrWhiteSpace(chineseName))
        {
            throw new InvalidOperationException("請輸入中文名。");
        }

        if (string.IsNullOrWhiteSpace(nickName))
        {
            throw new InvalidOperationException("請輸入暱稱。");
        }

        if (await _plantRepository.IsNickNameTakenAsync(nickName, null, cancellationToken))
        {
            throw new InvalidOperationException($"暱稱「{nickName}」已被使用，請換一個。");
        }

        PlantSpeciesModel species;
        if (confirmedSpecies != null && !string.IsNullOrWhiteSpace(confirmedSpecies.ScientificName))
        {
            species = await ResolveOrCreateSpeciesAsync(confirmedSpecies, chineseName, cancellationToken);
            try
            {
                // 若上層已 Ensure／Refresh 過則跳過；否則首次寫入知識
                await _plantKnowledgeService.SyncFromExternalAsync(species.ID, chineseName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Knowledge sync failed for species {SpeciesId}; plant will still be created", species.ID);
            }
        }
        else
        {
            species = await GetOrCreatePendingSpeciesAsync(chineseName, cancellationToken);
        }

        var utcNow = DateTime.UtcNow;
        var plant = new Midnight.EC.Plant.WEB.Models.Models.PlantModel
        {
            Name = chineseName,
            SpeciesID = species.ID,
            NickName = nickName,
            Description = draft.Description,
            Location = draft.Location,
            StartDate = draft.StartDate?.ToUniversalTime() ?? utcNow,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        await _plantRepository.InsertAsync(plant, cancellationToken);
        await _plantProfileService.SaveAsync(plant.ID, new PlantProfileDto {
            PlantId = plant.ID,
            WateringIntervalDays = DefaultWateringIntervalDays,
            ActualPlacement = draft.ActualPlacement,
            ActualLight = draft.ActualLight,
            HasRainCover = draft.HasRainCover,
            SubstrateType = draft.SubstrateType,
            SaucerState = draft.SaucerState,
            City = draft.City,
            EnvironmentMismatchAcknowledged = draft.EnvironmentMismatchAcknowledged
        }, cancellationToken);

        if (draft.WateredToday)
        {
            await _plantCareService.CreateAsync(
                plant.ID,
                DateTime.Today,
                CareRecordType.Watering,
                null,
                null,
                "建檔時標記今日已澆水",
                cancellationToken);
        }

        var created = await _plantRepository.GetByIdWithDetailsAsync(plant.ID, cancellationToken)
            ?? throw new InvalidOperationException("建立植物後無法讀取資料。");

        _logger.LogInformation("Created plant {PlantId} species {SpeciesId} pending={Pending}",
            created.ID, created.SpeciesID, species.SourceType == "Pending");
        return created.ToDto();
    }

    public async Task<PlantDto> RebindSpeciesAsync(
        Guid plantId,
        ExternalSpeciesResult confirmedSpecies,
        string? chineseNameHint,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(confirmedSpecies.ScientificName))
        {
            throw new InvalidOperationException("物種學名不可空白。");
        }

        var plant = await _plantRepository.GetByIdAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var chineseName = !string.IsNullOrWhiteSpace(chineseNameHint)
            ? chineseNameHint.Trim()
            : (plant.Name ?? string.Empty);

        var species = await ResolveOrCreateSpeciesAsync(confirmedSpecies, chineseName, cancellationToken);
        try
        {
            // 上層 ConfirmReselect 已 Ensure／強制同步；此處僅補首次寫入
            await _plantKnowledgeService.SyncFromExternalAsync(species.ID, chineseName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Knowledge sync failed for species {SpeciesId} during rebind", species.ID);
        }

        plant.SpeciesID = species.ID;
        if (!string.IsNullOrWhiteSpace(chineseName))
        {
            plant.Name = chineseName;
        }

        plant.ModifyDate = DateTime.UtcNow;
        await _plantRepository.UpdateAsync(plant, cancellationToken);

        var updated = await _plantRepository.GetByIdWithDetailsAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("換綁物種後無法讀取資料。");
        return updated.ToDto();
    }

    public async Task<PlantDto> UpdateAsync(
        Guid id,
        string name,
        string? nickName,
        string? location,
        string? description,
        DateTime? startDate,
        CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        if (!string.IsNullOrWhiteSpace(nickName) &&
            await _plantRepository.IsNickNameTakenAsync(nickName, id, cancellationToken))
        {
            throw new InvalidOperationException($"暱稱「{nickName}」已被使用，請換一個。");
        }

        plant.Name = name;
        plant.NickName = nickName;
        plant.Location = location;
        plant.Description = description;
        plant.StartDate = startDate?.ToUniversalTime() ?? plant.StartDate;
        plant.ModifyDate = DateTime.UtcNow;

        await _plantRepository.UpdateAsync(plant, cancellationToken);
        var updated = await _plantRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("更新植物後無法讀取資料。");

        return updated.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        // Spec: archive only (IsActive = false)
        plant.Enabled = false;
        plant.ModifyDate = DateTime.UtcNow;

        await _plantRepository.UpdateAsync(plant, cancellationToken);
    }

    private async Task<PlantSpeciesModel> ResolveOrCreateSpeciesAsync(
        ExternalSpeciesResult external,
        string chineseName,
        CancellationToken cancellationToken)
    {
        var existing = await _speciesRepository.GetByScientificNameExactAsync(external.ScientificName, cancellationToken);
        if (existing != null)
        {
            if (string.IsNullOrWhiteSpace(existing.ChineseName) && !string.IsNullOrWhiteSpace(chineseName))
            {
                existing.ChineseName = chineseName;
                existing.ModifyDate = DateTime.UtcNow;
                await _speciesRepository.UpdateAsync(existing, cancellationToken);
            }

            return existing;
        }

        var now = DateTime.UtcNow;
        var species = new PlantSpeciesModel
        {
            ScientificName = external.ScientificName.Trim(),
            CommonName = external.CommonName,
            ChineseName = external.ChineseName ?? chineseName,
            Genus = external.Genus,
            Family = external.Family,
            TaxonId = external.TaxonId,
            ImageUrl = external.ImageUrl,
            SourceType = external.SourceType,
            SourceId = external.SourceId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _speciesRepository.InsertAsync(species, cancellationToken);
        return species;
    }

    private async Task<PlantSpeciesModel> GetOrCreatePendingSpeciesAsync(string chineseName, CancellationToken cancellationToken)
    {
        var existing = await _speciesRepository.SearchByNameAsync(chineseName, cancellationToken);
        if (existing != null &&
            (string.Equals(existing.SourceType, "Pending", StringComparison.OrdinalIgnoreCase) ||
             existing.ScientificName == PendingScientificName))
        {
            return existing;
        }

        // Prefer a dedicated pending row per Chinese name when scientific is unknown
        var now = DateTime.UtcNow;
        var species = new PlantSpeciesModel
        {
            ScientificName = PendingScientificName,
            ChineseName = chineseName,
            CommonName = chineseName,
            SourceType = "Pending",
            SourceId = string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _speciesRepository.InsertAsync(species, cancellationToken);
        return species;
    }

    private async Task<List<PlantDto>> GetAllInternalAsync(CancellationToken cancellationToken)
    {
        var plants = await _plantRepository.GetAllActiveAsync(cancellationToken);
        return plants.Select(p => p.ToDto()).ToList();
    }

    private async Task<PlantDto?> GetByIdInternalAsync(Guid id, CancellationToken cancellationToken)
    {
        var plant = await _plantRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        return plant?.ToDto();
    }
}

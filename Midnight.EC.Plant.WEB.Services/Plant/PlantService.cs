using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.Plant;

public class PlantService : IPlantService
{
    public const int DefaultWateringIntervalDays = 7;
    public const string PendingScientificName = "未確認";

    private readonly IPlantRepository _plantRepository;
    private readonly IPlantSpeciesRepository _speciesRepository;
    private readonly IExternalPlantApiService _externalPlantApiService;
    private readonly IPlantKnowledgeService _plantKnowledgeService;
    private readonly IPlantProfileService _plantProfileService;
    private readonly IPlantCareService _plantCareService;
    private readonly ILogger<PlantService> _logger;

    public PlantService(
        IPlantRepository plantRepository,
        IPlantSpeciesRepository speciesRepository,
        IExternalPlantApiService externalPlantApiService,
        IPlantKnowledgeService plantKnowledgeService,
        IPlantProfileService plantProfileService,
        IPlantCareService plantCareService,
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

    public Task<PlantDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        GetByIdInternalAsync(id, cancellationToken);

    public Task<bool> IsNickNameTakenAsync(string nickName, int? excludePlantId = null, CancellationToken cancellationToken = default) =>
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

        PlantSpecies species;
        if (confirmedSpecies != null && !string.IsNullOrWhiteSpace(confirmedSpecies.ScientificName))
        {
            species = await ResolveOrCreateSpeciesAsync(confirmedSpecies, chineseName, cancellationToken);
            try
            {
                await _plantKnowledgeService.SyncFromExternalAsync(species.Id, chineseName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Knowledge sync failed for species {SpeciesId}; plant will still be created", species.Id);
            }
        }
        else
        {
            species = await GetOrCreatePendingSpeciesAsync(chineseName, cancellationToken);
        }

        var utcNow = DateTime.UtcNow;
        var plant = new Midnight.EC.Plant.WEB.Models.Entities.Plant
        {
            Name = chineseName,
            SpeciesId = species.Id,
            NickName = nickName,
            Description = draft.Description,
            Location = draft.Location,
            StartDate = draft.StartDate?.ToUniversalTime() ?? utcNow,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        await _plantRepository.AddAsync(plant, cancellationToken);
        await _plantRepository.SaveChangesAsync(cancellationToken);

        await _plantProfileService.SaveAsync(plant.Id, new PlantProfileDto
        {
            PlantId = plant.Id,
            WateringIntervalDays = DefaultWateringIntervalDays
        }, cancellationToken);

        if (draft.WateredToday)
        {
            await _plantCareService.CreateAsync(
                plant.Id,
                DateTime.Today,
                CareRecordType.Watering,
                null,
                null,
                "建檔時標記今日已澆水",
                cancellationToken);
        }

        var created = await _plantRepository.GetByIdWithDetailsAsync(plant.Id, cancellationToken)
            ?? throw new InvalidOperationException("建立植物後無法讀取資料。");

        _logger.LogInformation("Created plant {PlantId} species {SpeciesId} pending={Pending}",
            created.Id, created.SpeciesId, species.SourceType == "Pending");
        return created.ToDto();
    }

    public async Task<PlantDto> UpdateAsync(
        int id,
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
        plant.UpdatedAt = DateTime.UtcNow;

        await _plantRepository.UpdateAsync(plant, cancellationToken);
        await _plantRepository.SaveChangesAsync(cancellationToken);

        var updated = await _plantRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("更新植物後無法讀取資料。");

        return updated.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        // Spec: archive only (IsActive = false)
        plant.IsActive = false;
        plant.UpdatedAt = DateTime.UtcNow;

        await _plantRepository.UpdateAsync(plant, cancellationToken);
        await _plantRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<PlantSpecies> ResolveOrCreateSpeciesAsync(
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
                existing.UpdatedAt = DateTime.UtcNow;
                await _speciesRepository.UpdateAsync(existing, cancellationToken);
                await _speciesRepository.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        var now = DateTime.UtcNow;
        var species = new PlantSpecies
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

        await _speciesRepository.AddAsync(species, cancellationToken);
        await _speciesRepository.SaveChangesAsync(cancellationToken);
        return species;
    }

    private async Task<PlantSpecies> GetOrCreatePendingSpeciesAsync(string chineseName, CancellationToken cancellationToken)
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
        var species = new PlantSpecies
        {
            ScientificName = PendingScientificName,
            ChineseName = chineseName,
            CommonName = chineseName,
            SourceType = "Pending",
            SourceId = string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _speciesRepository.AddAsync(species, cancellationToken);
        await _speciesRepository.SaveChangesAsync(cancellationToken);
        return species;
    }

    private async Task<List<PlantDto>> GetAllInternalAsync(CancellationToken cancellationToken)
    {
        var plants = await _plantRepository.GetAllActiveAsync(cancellationToken);
        return plants.Select(p => p.ToDto()).ToList();
    }

    private async Task<PlantDto?> GetByIdInternalAsync(int id, CancellationToken cancellationToken)
    {
        var plant = await _plantRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        return plant?.ToDto();
    }
}

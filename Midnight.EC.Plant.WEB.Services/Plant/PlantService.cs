using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.Plant;

public class PlantService : IPlantService
{
    private readonly IPlantRepository _plantRepository;
    private readonly IPlantSpeciesRepository _speciesRepository;
    private readonly IExternalPlantApiService _externalPlantApiService;
    private readonly IPlantKnowledgeService _plantKnowledgeService;
    private readonly ILogger<PlantService> _logger;

    public PlantService(
        IPlantRepository plantRepository,
        IPlantSpeciesRepository speciesRepository,
        IExternalPlantApiService externalPlantApiService,
        IPlantKnowledgeService plantKnowledgeService,
        ILogger<PlantService> logger)
    {
        _plantRepository = plantRepository;
        _speciesRepository = speciesRepository;
        _externalPlantApiService = externalPlantApiService;
        _plantKnowledgeService = plantKnowledgeService;
        _logger = logger;
    }

    public Task<List<PlantDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return GetAllInternalAsync(cancellationToken);
    }

    public Task<PlantDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return GetByIdInternalAsync(id, cancellationToken);
    }

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
        var displayKeyword = speciesKeyword?.Trim();
        var searchKeyword = displayKeyword ?? string.Empty;

        if (identificationImage != null)
        {
            var identified = await _externalPlantApiService.IdentifyFromImageAsync(
                identificationImage,
                identificationFileName ?? "plant.jpg",
                cancellationToken);
            if (identified != null)
            {
                searchKeyword = identified.ScientificName;
                _logger.LogInformation("Create plant: identified {ScientificName} from photo", identified.ScientificName);
            }
            else if (string.IsNullOrWhiteSpace(searchKeyword))
            {
                throw new InvalidOperationException("無法從照片辨識植物，請輸入名稱或上傳更清晰的照片。");
            }
        }

        if (string.IsNullOrWhiteSpace(searchKeyword))
        {
            throw new InvalidOperationException("請輸入植物名稱或上傳照片。");
        }

        var species = await _speciesRepository.SearchByNameAsync(searchKeyword, cancellationToken)
            ?? await _speciesRepository.SearchByNameAsync(displayKeyword ?? searchKeyword, cancellationToken);

        if (species == null)
        {
            var external = await _externalPlantApiService.SearchSpeciesAsync(searchKeyword, cancellationToken)
                ?? throw new InvalidOperationException("找不到對應的植物物種資料。");

            var now = DateTime.UtcNow;
            species = new PlantSpecies
            {
                ScientificName = external.Species!.ScientificName,
                CommonName = external.Species.CommonName,
                ChineseName = external.Species.ChineseName ?? displayKeyword,
                Genus = external.Species.Genus,
                Family = external.Species.Family,
                TaxonId = external.Species.TaxonId,
                ImageUrl = external.Species.ImageUrl,
                SourceType = external.Species.SourceType,
                SourceId = external.Species.SourceId,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _speciesRepository.AddAsync(species, cancellationToken);
            await _speciesRepository.SaveChangesAsync(cancellationToken);
        }

        if (identificationImage != null)
        {
            await _plantKnowledgeService.RefreshFromExternalAsync(species.Id, displayKeyword ?? searchKeyword, cancellationToken);
        }
        else
        {
            await _plantKnowledgeService.SyncFromExternalAsync(species.Id, displayKeyword ?? searchKeyword, cancellationToken);
        }

        var utcNow = DateTime.UtcNow;
        var plant = new Midnight.EC.Plant.WEB.Models.Entities.Plant
        {
            Name = name,
            SpeciesId = species.Id,
            NickName = nickName,
            Description = description,
            Location = location,
            StartDate = startDate?.ToUniversalTime() ?? utcNow,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        await _plantRepository.AddAsync(plant, cancellationToken);
        await _plantRepository.SaveChangesAsync(cancellationToken);

        var created = await _plantRepository.GetByIdWithDetailsAsync(plant.Id, cancellationToken)
            ?? throw new InvalidOperationException("建立植物後無法讀取資料。");

        _logger.LogInformation("Created plant {PlantId} for species {SpeciesId}", created.Id, created.SpeciesId);
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

        plant.IsActive = false;
        plant.UpdatedAt = DateTime.UtcNow;

        await _plantRepository.UpdateAsync(plant, cancellationToken);
        await _plantRepository.SaveChangesAsync(cancellationToken);
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

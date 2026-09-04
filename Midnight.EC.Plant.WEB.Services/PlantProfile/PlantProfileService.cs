using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Entities;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.PlantProfile;

public class PlantProfileService : IPlantProfileService
{
    private readonly IPlantProfileRepository _profileRepository;
    private readonly IPlantRepository _plantRepository;
    private readonly ILogger<PlantProfileService> _logger;

    public PlantProfileService(
        IPlantProfileRepository profileRepository,
        IPlantRepository plantRepository,
        ILogger<PlantProfileService> logger)
    {
        _profileRepository = profileRepository;
        _plantRepository = plantRepository;
        _logger = logger;
    }

    public async Task<PlantProfileDto?> GetByPlantIdAsync(int plantId, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByPlantIdAsync(plantId, cancellationToken);
        return profile?.ToDto();
    }

    public async Task<PlantProfileDto> SaveAsync(int plantId, PlantProfileDto model, CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        var profile = await _profileRepository.GetByPlantIdAsync(plantId, cancellationToken);
        var now = DateTime.UtcNow;

        if (profile == null)
        {
            profile = new Midnight.EC.Plant.WEB.Models.Entities.PlantProfile
            {
                PlantId = plant.Id,
                CreatedAt = now
            };
            MapProfile(profile, model, now);
            await _profileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            MapProfile(profile, model, now);
            await _profileRepository.UpdateAsync(profile, cancellationToken);
        }

        await _profileRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Saved plant profile for plant {PlantId}", plantId);
        return profile.ToDto();
    }

    private static void MapProfile(Midnight.EC.Plant.WEB.Models.Entities.PlantProfile profile, PlantProfileDto model, DateTime now)
    {
        profile.WateringIntervalDays = model.WateringIntervalDays;
        profile.FertilizingIntervalDays = model.FertilizingIntervalDays;
        profile.TargetHumidityMin = model.TargetHumidityMin;
        profile.TargetHumidityMax = model.TargetHumidityMax;
        profile.TargetTemperatureMin = model.TargetTemperatureMin;
        profile.TargetTemperatureMax = model.TargetTemperatureMax;
        profile.PersonalCareNotes = model.PersonalCareNotes;
        profile.ActualPlacement = model.ActualPlacement;
        profile.ActualLight = model.ActualLight;
        profile.HasRainCover = model.HasRainCover;
        profile.SubstrateType = model.SubstrateType;
        profile.City = model.City;
        profile.OverrideSuggestedLight = model.OverrideSuggestedLight;
        profile.OverrideCareTaboosJson = model.OverrideCareTaboosJson;
        profile.WateringIntervalDetachedFromWiki = model.WateringIntervalDetachedFromWiki;
        profile.EnvironmentMismatchAcknowledged = model.EnvironmentMismatchAcknowledged;
        profile.UpdatedAt = now;
    }
}

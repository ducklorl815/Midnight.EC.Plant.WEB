using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Extensions;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Services.External;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.PlantList;

public class PlantListService
{
    private const int DefaultFertilizingIntervalDays = 30;

    private readonly PlantRespo _plantRepository;
    private readonly PlantProfileRespo _profileRepository;
    private readonly PlantCareRecordRespo _careRepository;
    private readonly PlantFertilizerProductRespo _fertilizerRepository;
    private readonly PlantAnalysisRespo _analysisRepository;
    private readonly PlantImageRespo _imageRepository;
    private readonly IImageStorageService _imageStorage;

    public PlantListService(
        PlantRespo plantRepository,
        PlantProfileRespo profileRepository,
        PlantCareRecordRespo careRepository,
        PlantFertilizerProductRespo fertilizerRepository,
        PlantAnalysisRespo analysisRepository,
        PlantImageRespo imageRepository,
        IImageStorageService imageStorage)
    {
        _plantRepository = plantRepository;
        _profileRepository = profileRepository;
        _careRepository = careRepository;
        _fertilizerRepository = fertilizerRepository;
        _analysisRepository = analysisRepository;
        _imageRepository = imageRepository;
        _imageStorage = imageStorage;
    }

    public async Task<List<PlantListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var plants = await _plantRepository.GetAllActiveAsync(cancellationToken);
        if (plants.Count == 0)
            return [];

        var plantIds = plants.Select(p => p.ID).ToList();
        var profiles = await _profileRepository.GetByPlantIdsAsync(plantIds, cancellationToken);
        var lastWatering = await _careRepository.GetLastWateringDatesAsync(plantIds, cancellationToken);
        var lastFertilizing = await _careRepository.GetLastFertilizingDatesAsync(plantIds, cancellationToken);
        var productsByPlant = await _fertilizerRepository.GetByPlantIdsAsync(plantIds, cancellationToken);
        var allProductIds = productsByPlant.Values.SelectMany(v => v).Select(p => p.ID).ToList();
        var lastByProduct = await _careRepository.GetLastFertilizingDatesByProductIdsAsync(allProductIds, cancellationToken);
        var covers = await _imageRepository.GetCoversByPlantIdsAsync(plantIds, cancellationToken);
        var today = DateTime.Today;

        var items = new List<PlantListItemDto>(plants.Count);
        foreach (var plant in plants)
        {
            profiles.TryGetValue(plant.ID, out var profile);
            lastWatering.TryGetValue(plant.ID, out var lastWaterDate);
            var hasWater = lastWatering.ContainsKey(plant.ID);
            var waterInterval = profile?.WateringIntervalDays
                ?? CareConstraintExtractor.InferWateringIntervalDays(plant.Species?.Knowledge?.WaterRequirement);

            int? waterRemaining = null;
            if (waterInterval is > 0)
            {
                var last = hasWater ? lastWaterDate.Date : plant.CreateDate.Date;
                waterRemaining = waterInterval.Value - (today - last).Days;
            }

            productsByPlant.TryGetValue(plant.ID, out var products);
            products ??= [];

            var fertilizerRows = new List<PlantListFertilizerItemDto>();
            if (products.Count > 0)
            {
                foreach (var product in products)
                {
                    lastByProduct.TryGetValue(product.ID, out var lastFertDate);
                    var hasFert = lastByProduct.ContainsKey(product.ID);
                    var last = hasFert ? lastFertDate.Date : plant.CreateDate.Date;
                    var interval = product.IntervalDays > 0 ? product.IntervalDays : DefaultFertilizingIntervalDays;
                    fertilizerRows.Add(new PlantListFertilizerItemDto
                    {
                        FertilizerProductId = product.ID,
                        Name = product.Name,
                        IntervalDays = interval,
                        LastFertilizedDate = hasFert ? lastFertDate.Date : null,
                        DaysRemaining = interval - (today - last).Days
                    });
                }
            }
            else
            {
                var interval = profile?.FertilizingIntervalDays ?? DefaultFertilizingIntervalDays;
                lastFertilizing.TryGetValue(plant.ID, out var lastFertDate);
                var hasFert = lastFertilizing.ContainsKey(plant.ID);
                var last = hasFert ? lastFertDate.Date : plant.CreateDate.Date;
                fertilizerRows.Add(new PlantListFertilizerItemDto
                {
                    FertilizerProductId = null,
                    Name = "施肥",
                    IntervalDays = interval,
                    LastFertilizedDate = hasFert ? lastFertDate.Date : null,
                    DaysRemaining = interval - (today - last).Days
                });
            }

            var analyses = await _analysisRepository.GetByPlantIdAsync(plant.ID, cancellationToken);
            var latestHealth = analyses.OrderByDescending(a => a.CreateDate).FirstOrDefault()?.HealthScore;

            covers.TryGetValue(plant.ID, out var cover);
            items.Add(new PlantListItemDto
            {
                PlantId = plant.ID,
                DisplayName = !string.IsNullOrWhiteSpace(plant.NickName) ? plant.NickName! : plant.Name,
                CoverImagePath = cover == null ? null : _imageStorage.GetPublicPath(cover.StoragePath),
                LatestHealthScore = latestHealth,
                WaterDaysRemaining = waterRemaining,
                WateringIntervalDays = waterInterval,
                LastWateringDate = hasWater ? lastWaterDate.Date : null,
                Fertilizers = fertilizerRows
            });
        }

        return items
            .OrderBy(i => i.WaterDaysRemaining ?? int.MaxValue)
            .ThenBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<List<PlantFertilizerProductDto>> GetFertilizersForPlantAsync(
        Guid plantId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _fertilizerRepository.GetByPlantIdAsync(plantId, cancellationToken);
        return rows.Select(r => r.ToDto()).ToList();
    }

    public async Task<PlantFertilizerProductDto> AddFertilizerAsync(
        Guid plantId,
        string name,
        int intervalDays,
        CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(plantId, cancellationToken)
            ?? throw new InvalidOperationException("找不到植物。");

        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("請輸入肥料名稱。");
        if (intervalDays < 1)
            throw new InvalidOperationException("施肥間隔至少 1 天。");

        var existing = await _fertilizerRepository.GetByPlantIdAsync(plant.Id, cancellationToken);
        var product = new Midnight.EC.Plant.WEB.Models.Models.PlantFertilizerProductModel
        {
            PlantID = plant.Id,
            Name = name.Length > 64 ? name[..64] : name,
            IntervalDays = intervalDays,
            SortOrder = existing.Count == 0 ? 0 : existing.Max(x => x.SortOrder) + 1
        };
        await _fertilizerRepository.InsertAsync(product, cancellationToken);
        return product.ToDto();
    }

    public async Task UpdateFertilizerAsync(
        Guid plantId,
        Guid fertilizerProductId,
        string name,
        int intervalDays,
        CancellationToken cancellationToken = default)
    {
        var product = await _fertilizerRepository.GetByIdAsync(fertilizerProductId, cancellationToken)
            ?? throw new InvalidOperationException("找不到此肥料。");
        if (product.PlantID != plantId)
            throw new InvalidOperationException("肥料不屬於此植栽。");

        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("請輸入肥料名稱。");
        if (intervalDays < 1)
            throw new InvalidOperationException("施肥間隔至少 1 天。");

        product.Name = name.Length > 64 ? name[..64] : name;
        product.IntervalDays = intervalDays;
        await _fertilizerRepository.UpdateAsync(product, cancellationToken);
    }

    public async Task DeleteFertilizerAsync(
        Guid plantId,
        Guid fertilizerProductId,
        CancellationToken cancellationToken = default)
    {
        var product = await _fertilizerRepository.GetByIdAsync(fertilizerProductId, cancellationToken)
            ?? throw new InvalidOperationException("找不到此肥料。");
        if (product.PlantID != plantId)
            throw new InvalidOperationException("肥料不屬於此植栽。");

        await _fertilizerRepository.SoftDeleteAsync(fertilizerProductId, cancellationToken);
    }
}

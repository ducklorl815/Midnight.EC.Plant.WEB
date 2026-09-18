namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantListItemDto
{
    public Guid PlantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? CoverImagePath { get; set; }
    public int? LatestHealthScore { get; set; }
    /// <summary>澆水：還需天數（&gt;0 還需、0 今天、&lt;0 已過）。無週期時為 null。</summary>
    public int? WaterDaysRemaining { get; set; }
    public int? WateringIntervalDays { get; set; }
    public DateTime? LastWateringDate { get; set; }
    public List<PlantListFertilizerItemDto> Fertilizers { get; set; } = [];
}

public class PlantListFertilizerItemDto
{
    public Guid? FertilizerProductId { get; set; }
    public string Name { get; set; } = "施肥";
    public int IntervalDays { get; set; }
    public int? DaysRemaining { get; set; }
    public DateTime? LastFertilizedDate { get; set; }
}

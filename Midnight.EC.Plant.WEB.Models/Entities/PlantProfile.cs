namespace Midnight.EC.Plant.WEB.Models.Entities;

public class PlantProfile
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public int? WateringIntervalDays { get; set; }
    public int? FertilizingIntervalDays { get; set; }
    public decimal? TargetHumidityMin { get; set; }
    public decimal? TargetHumidityMax { get; set; }
    public decimal? TargetTemperatureMin { get; set; }
    public decimal? TargetTemperatureMax { get; set; }
    public string? PersonalCareNotes { get; set; }

    /// <summary>實際：室內／室外／陽台</summary>
    public Enums.PlacementType? ActualPlacement { get; set; }
    /// <summary>實際日照四檔</summary>
    public Enums.LightLevel? ActualLight { get; set; }
    public bool? HasRainCover { get; set; }
    public string? SubstrateType { get; set; }
    /// <summary>台灣縣市</summary>
    public string? City { get; set; }

    /// <summary>單盆覆寫：建議光照（可與物種知識脫鉤）</summary>
    public Enums.LightLevel? OverrideSuggestedLight { get; set; }
    /// <summary>單盆覆寫禁忌（JSON 字串陣列）；null=跟物種</summary>
    public string? OverrideCareTaboosJson { get; set; }
    /// <summary>提醒週期是否已與 Wiki 建議脫鉤</summary>
    public bool WateringIntervalDetachedFromWiki { get; set; }
    public bool EnvironmentMismatchAcknowledged { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Plant Plant { get; set; } = null!;
}

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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Plant Plant { get; set; } = null!;
}

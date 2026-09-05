using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantProfileModel
{
    public Guid ID { get; set; }
    public Guid Id { get => ID; set => ID = value; }
    public int Seq { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime CreatedAt { get => CreateDate; set => CreateDate = value; }
    public DateTime ModifyDate { get; set; }
    public DateTime UpdatedAt { get => ModifyDate; set => ModifyDate = value; }
    public bool Enabled { get; set; } = true;
    public bool IsActive { get => Enabled; set => Enabled = value; }
    public bool Deleted { get; set; }
    public Guid PlantID { get; set; }
    public Guid PlantId { get => PlantID; set => PlantID = value; }
    public int? WateringIntervalDays { get; set; }
    public int? FertilizingIntervalDays { get; set; }
    public decimal? TargetHumidityMin { get; set; }
    public decimal? TargetHumidityMax { get; set; }
    public decimal? TargetTemperatureMin { get; set; }
    public decimal? TargetTemperatureMax { get; set; }
    public string? PersonalCareNotes { get; set; }
    public PlacementType? ActualPlacement { get; set; }
    public LightLevel? ActualLight { get; set; }
    public bool? HasRainCover { get; set; }
    public string? SubstrateType { get; set; }
    public SaucerState? SaucerState { get; set; }
    public string? City { get; set; }
    public LightLevel? OverrideSuggestedLight { get; set; }
    public string? OverrideCareTaboosJson { get; set; }
    public bool WateringIntervalDetachedFromWiki { get; set; }
    public bool EnvironmentMismatchAcknowledged { get; set; }
    public string? AiEnvironmentAdvice { get; set; }
}

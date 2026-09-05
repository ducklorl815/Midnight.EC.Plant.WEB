using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantKnowledgeModel
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
    public Guid SpeciesID { get; set; }
    public Guid SpeciesId { get => SpeciesID; set => SpeciesID = value; }
    public string? LightRequirement { get; set; }
    public string? WaterRequirement { get; set; }
    public string? HumidityRequirement { get; set; }
    public decimal? TemperatureMin { get; set; }
    public decimal? TemperatureMax { get; set; }
    public string? SoilRequirement { get; set; }
    public string? FertilizerRequirement { get; set; }
    public string? Dormancy { get; set; }
    public string? GrowthSeason { get; set; }
    public string? RepottingAdvice { get; set; }
    public string? CommonProblems { get; set; }
    public string? PestProblems { get; set; }
    public string? DiseaseProblems { get; set; }
    public string? CareSummary { get; set; }
    public string? ExternalCareGuide { get; set; }
    public LightLevel? SuggestedLight { get; set; }
    public string? CareTaboosJson { get; set; }
    public int? SuggestedWateringIntervalDays { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public int DataVersion { get; set; } = 1;
}

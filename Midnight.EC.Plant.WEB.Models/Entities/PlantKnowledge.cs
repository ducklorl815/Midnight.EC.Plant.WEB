namespace Midnight.EC.Plant.WEB.Models.Entities;

public class PlantKnowledge
{
    public int Id { get; set; }
    public int SpeciesId { get; set; }
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
    /// <summary>外部來源整合後的可讀照護說明（繁體中文敘述）</summary>
    public string? ExternalCareGuide { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public int DataVersion { get; set; } = 1;
    public DateTime UpdatedAt { get; set; }

    public PlantSpecies Species { get; set; } = null!;
}

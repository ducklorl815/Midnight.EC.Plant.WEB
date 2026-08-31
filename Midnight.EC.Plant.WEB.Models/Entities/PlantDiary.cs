namespace Midnight.EC.Plant.WEB.Models.Entities;

public class PlantDiary
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public DateTime DiaryDate { get; set; }
    public string? Title { get; set; }
    public string? Note { get; set; }
    public string? WeatherNote { get; set; }
    public string? EnvironmentNote { get; set; }
    public string? WateringNote { get; set; }
    public string? FertilizerNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Plant Plant { get; set; } = null!;
    public ICollection<PlantImage> Images { get; set; } = [];
    public ICollection<PlantAnalysis> Analyses { get; set; } = [];
    public ICollection<PlantAnalysisJob> AnalysisJobs { get; set; } = [];
}

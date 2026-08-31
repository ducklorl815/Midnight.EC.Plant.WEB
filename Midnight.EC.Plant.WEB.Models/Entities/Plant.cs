namespace Midnight.EC.Plant.WEB.Models.Entities;

public class Plant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SpeciesId { get; set; }
    public string? NickName { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? EnvironmentNote { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? StartDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public PlantSpecies Species { get; set; } = null!;
    public ICollection<PlantDiary> Diaries { get; set; } = [];
    public ICollection<PlantImage> Images { get; set; } = [];
    public ICollection<PlantAnalysis> Analyses { get; set; } = [];
    public ICollection<PlantAnalysisJob> AnalysisJobs { get; set; } = [];
    public ICollection<PlantCareRecord> CareRecords { get; set; } = [];
    public PlantProfile? Profile { get; set; }
    public ICollection<PlantReminder> Reminders { get; set; } = [];
}

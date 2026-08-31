using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Entities;

public class PlantKnowledgeSyncLog
{
    public int Id { get; set; }
    public int SpeciesId { get; set; }
    public ExternalPlantApiProvider Provider { get; set; }
    public string? RequestUrl { get; set; }
    public SyncStatus Status { get; set; }
    public string? ResponseHash { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public PlantSpecies Species { get; set; } = null!;
}

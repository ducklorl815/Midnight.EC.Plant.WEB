using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Entities;

public class PlantCareRecord
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public DateTime RecordDate { get; set; }
    public CareRecordType CareType { get; set; }
    public decimal? NumericValue { get; set; }
    public string? Unit { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public Entities.Plant Plant { get; set; } = null!;
}

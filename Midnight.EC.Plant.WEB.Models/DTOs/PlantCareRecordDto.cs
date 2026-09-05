using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantCareRecordDto
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public DateTime RecordDate { get; set; }
    public CareRecordType CareType { get; set; }
    public decimal? NumericValue { get; set; }
    public string? Unit { get; set; }
    public string? Note { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime CreatedAt { get => CreateDate; set => CreateDate = value; }
}

public class PlantTrendDto
{
    public List<string> Labels { get; set; } = [];
    public List<int?> HealthScores { get; set; } = [];
    public List<decimal?> Temperatures { get; set; } = [];
    public List<decimal?> Humidities { get; set; } = [];
    public List<decimal?> LightLevels { get; set; } = [];
    public List<int> WateringCounts { get; set; } = [];
    public DateTime? LastWateringDate { get; set; }
    public int TotalWateringCount { get; set; }
}

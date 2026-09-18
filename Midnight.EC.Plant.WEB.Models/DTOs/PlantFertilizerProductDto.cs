namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantFertilizerProductDto
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int IntervalDays { get; set; }
    public int SortOrder { get; set; }
}

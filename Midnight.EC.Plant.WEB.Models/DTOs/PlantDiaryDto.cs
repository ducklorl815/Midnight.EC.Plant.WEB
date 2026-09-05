namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantDiaryDto
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public DateTime DiaryDate { get; set; }
    public string? Title { get; set; }
    public string? Note { get; set; }
    public string? WeatherNote { get; set; }
    public string? EnvironmentNote { get; set; }
    public string? WateringNote { get; set; }
    public string? FertilizerNote { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime CreatedAt { get => CreateDate; set => CreateDate = value; }
    public List<PlantImageDto> Images { get; set; } = [];
}

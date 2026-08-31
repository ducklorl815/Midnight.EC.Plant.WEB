namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantDiaryDto
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
    public List<PlantImageDto> Images { get; set; } = [];
}

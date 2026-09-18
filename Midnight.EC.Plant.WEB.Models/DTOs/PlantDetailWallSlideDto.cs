namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantDetailWallSlideDto
{
    public Guid PlantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ScientificName { get; set; }
    public string Intro { get; set; } = "尚無介紹";
    public Guid? CoverImageId { get; set; }
    public string? CoverImagePath { get; set; }
    public Guid? LatestEffectImageId { get; set; }
    public string? LatestEffectImageUrl { get; set; }
    public string? LeftImagePath { get; set; }
    public double CardX { get; set; } = 6;
    public double CardY { get; set; } = 22;
    public List<PlantDetailWallCareFactDto> CareFacts { get; set; } = [];
}

public class PlantDetailWallCareFactDto
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = "未設定";
}

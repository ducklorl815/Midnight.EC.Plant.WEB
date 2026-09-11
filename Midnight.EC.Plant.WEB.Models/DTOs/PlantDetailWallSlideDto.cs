namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantDetailWallSlideDto
{
    public Guid PlantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ScientificName { get; set; }
    public string Intro { get; set; } = "尚無介紹";
    public string? CoverImagePath { get; set; }
    /// <summary>Display scale within the frame.</summary>
    public double Zoom { get; set; } = 1;
    /// <summary>Focus X as percent 0..100 (50 = centered pan).</summary>
    public double FocusX { get; set; } = 50;
    /// <summary>Focus Y as percent 0..100 (50 = centered pan).</summary>
    public double FocusY { get; set; } = 50;
    public List<PlantDetailWallCareFactDto> CareFacts { get; set; } = [];
}

public class PlantDetailWallCareFactDto
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = "未設定";
}

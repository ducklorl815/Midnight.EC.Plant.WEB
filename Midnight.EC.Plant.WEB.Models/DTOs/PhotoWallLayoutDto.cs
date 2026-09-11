using System.Text.Json.Serialization;

namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PhotoWallLayoutDto
{
    public const int CurrentVersion = 1;
    public const int DefaultColumns = 4;
    public const int DefaultGapPx = 14;
    public const string DefaultKey = "default";

    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    [JsonPropertyName("columns")]
    public int Columns { get; set; } = DefaultColumns;

    [JsonPropertyName("gapX")]
    public int GapX { get; set; } = DefaultGapPx;

    [JsonPropertyName("gapY")]
    public int GapY { get; set; } = DefaultGapPx;

    [JsonPropertyName("tiles")]
    public List<PhotoWallTileDto> Tiles { get; set; } = [];

    /// <summary>Per vertical seam widths (between columns). Length ideally columns-1.</summary>
    [JsonPropertyName("colGaps")]
    public List<int>? ColGaps { get; set; }

    /// <summary>Per horizontal seam widths (between rows). Length ideally rowCount-1.</summary>
    [JsonPropertyName("rowGaps")]
    public List<int>? RowGaps { get; set; }

    /// <summary>v2 reserved: left-right diagonal pairs.</summary>
    [JsonPropertyName("diagonals")]
    public List<PhotoWallDiagonalDto>? Diagonals { get; set; }
}

public class PhotoWallTileDto
{
    [JsonPropertyName("plantId")]
    public Guid PlantId { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("colSpan")]
    public int ColSpan { get; set; } = 1;

    [JsonPropertyName("rowSpan")]
    public int RowSpan { get; set; } = 1;

    /// <summary>Display scale within the tile frame (&gt;0). Above 1 may crop; below 1 letterboxes.</summary>
    [JsonPropertyName("zoom")]
    public double Zoom { get; set; } = 1;

    /// <summary>Focus X as percent 0..100. Default 0 = left.</summary>
    [JsonPropertyName("focusX")]
    public double FocusX { get; set; } = 0;

    /// <summary>Focus Y as percent 0..100. Default 100 = bottom.</summary>
    [JsonPropertyName("focusY")]
    public double FocusY { get; set; } = 100;
}

public class PhotoWallDiagonalDto
{
    [JsonPropertyName("leftPlantId")]
    public Guid LeftPlantId { get; set; }

    [JsonPropertyName("rightPlantId")]
    public Guid RightPlantId { get; set; }

    /// <summary>-1..1 cut-line offset; 0 = equal triangles.</summary>
    [JsonPropertyName("offset")]
    public double Offset { get; set; }
}

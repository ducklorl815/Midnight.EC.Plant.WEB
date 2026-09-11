using Midnight.EC.Plant.WEB.Models.DTOs;

namespace Midnight.EC.Plant.WEB.Services.Plant;

public static class PhotoWallPacker
{
    public const int ContentRowPx = 140;

    public sealed record PackedTile(
        Guid PlantId,
        int Col,
        int Row,
        int ColSpan,
        int RowSpan,
        double Zoom,
        double FocusX,
        double FocusY,
        int Order);

    public static List<PackedTile> Pack(IEnumerable<PhotoWallTileDto> tiles, int columns)
    {
        var ordered = tiles.OrderBy(t => t.Order).ToList();
        var maxSpan = ordered.Count == 0 ? 1 : ordered.Max(t => Math.Max(1, t.ColSpan));
        columns = Math.Max(2, Math.Max(columns <= 0 ? PhotoWallLayoutDto.DefaultColumns : columns, maxSpan));
        var occupied = new HashSet<(int R, int C)>();
        var result = new List<PackedTile>();

        foreach (var tile in ordered)
        {
            var colSpan = Math.Max(1, tile.ColSpan);
            var rowSpan = Math.Max(1, tile.RowSpan);
            var placed = false;

            for (var row = 0; !placed; row++)
            {
                for (var col = 0; col <= columns - colSpan; col++)
                {
                    if (!Fits(occupied, row, col, rowSpan, colSpan))
                        continue;

                    Mark(occupied, row, col, rowSpan, colSpan);
                    result.Add(new PackedTile(
                        tile.PlantId,
                        col,
                        row,
                        colSpan,
                        rowSpan,
                        ClampZoom(tile.Zoom),
                        ClampFocus(tile.FocusX),
                        ClampFocus(tile.FocusY),
                        tile.Order));
                    placed = true;
                    break;
                }
            }
        }

        return result;
    }

    public static string BuildColumnTemplate(PhotoWallLayoutDto layout)
    {
        var cols = Math.Max(2, layout.Columns <= 0 ? PhotoWallLayoutDto.DefaultColumns : layout.Columns);
        var parts = new List<string>();
        for (var i = 0; i < cols; i++)
        {
            parts.Add("1fr");
            if (i < cols - 1)
                parts.Add($"{ResolveColGap(layout, i)}px");
        }
        return string.Join(" ", parts);
    }

    public static string BuildRowTemplate(PhotoWallLayoutDto layout, int contentRowCount, int rowPx = ContentRowPx)
    {
        contentRowCount = Math.Max(1, contentRowCount);
        var parts = new List<string>();
        for (var i = 0; i < contentRowCount; i++)
        {
            parts.Add($"{rowPx}px");
            if (i < contentRowCount - 1)
                parts.Add($"{ResolveRowGap(layout, i)}px");
        }
        return string.Join(" ", parts);
    }

    public static int ContentLine(int contentIndex) => contentIndex * 2 + 1;
    public static int SpanTracks(int span) => span * 2 - 1;
    public static int GapLine(int gapIndex) => gapIndex * 2 + 2;

    public static int ResolveColGap(PhotoWallLayoutDto layout, int index)
    {
        if (layout.ColGaps != null && index >= 0 && index < layout.ColGaps.Count)
            return ClampGap(layout.ColGaps[index]);
        return ClampGap(layout.GapX);
    }

    public static int ResolveRowGap(PhotoWallLayoutDto layout, int index)
    {
        if (layout.RowGaps != null && index >= 0 && index < layout.RowGaps.Count)
            return ClampGap(layout.RowGaps[index]);
        return ClampGap(layout.GapY);
    }

    public static List<int> NormalizeColGaps(PhotoWallLayoutDto layout)
    {
        var n = Math.Max(0, Math.Max(2, layout.Columns <= 0 ? PhotoWallLayoutDto.DefaultColumns : layout.Columns) - 1);
        var list = new List<int>(n);
        for (var i = 0; i < n; i++)
            list.Add(ResolveColGap(layout, i));
        return list;
    }

    public static List<int> NormalizeRowGaps(PhotoWallLayoutDto layout, int contentRowCount)
    {
        var n = Math.Max(0, contentRowCount - 1);
        var list = new List<int>(n);
        for (var i = 0; i < n; i++)
            list.Add(ResolveRowGap(layout, i));
        return list;
    }

    private static bool Fits(HashSet<(int R, int C)> occupied, int row, int col, int rowSpan, int colSpan)
    {
        for (var r = row; r < row + rowSpan; r++)
        for (var c = col; c < col + colSpan; c++)
            if (occupied.Contains((r, c)))
                return false;
        return true;
    }

    private static void Mark(HashSet<(int R, int C)> occupied, int row, int col, int rowSpan, int colSpan)
    {
        for (var r = row; r < row + rowSpan; r++)
        for (var c = col; c < col + colSpan; c++)
            occupied.Add((r, c));
    }

    private static int ClampGap(int value) => Math.Max(0, value);
    private static double ClampZoom(double value) => value <= 0 || double.IsNaN(value) || double.IsInfinity(value) ? 1 : value;
    private static double ClampFocus(double value) => double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 100);
}

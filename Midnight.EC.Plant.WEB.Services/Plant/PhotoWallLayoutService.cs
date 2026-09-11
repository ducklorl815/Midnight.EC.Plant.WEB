using System.Text.Json;
using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Models.Respository;

namespace Midnight.EC.Plant.WEB.Services.Plant;

public class PhotoWallLayoutService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly PhotoWallLayoutRespo _layoutRespo;
    private readonly ILogger<PhotoWallLayoutService> _logger;

    public PhotoWallLayoutService(
        PhotoWallLayoutRespo layoutRespo,
        ILogger<PhotoWallLayoutService> logger)
    {
        _layoutRespo = layoutRespo;
        _logger = logger;
    }

    public async Task<PhotoWallLayoutDto> GetMergedLayoutAsync(
        IReadOnlyList<PlantWallSeed> plants,
        CancellationToken cancellationToken = default)
    {
        var stored = await TryLoadAsync(cancellationToken);
        if (stored == null)
            return SeedDefault(plants);

        return Merge(stored, plants);
    }

    public async Task SaveAsync(
        PhotoWallLayoutDto layout,
        IReadOnlySet<Guid> validPlantIds,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(layout, validPlantIds);
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        await _layoutRespo.UpsertAsync(new PhotoWallLayoutModel
        {
            LayoutKey = PhotoWallLayoutDto.DefaultKey,
            LayoutJson = json
        }, cancellationToken);
    }

    private async Task<PhotoWallLayoutDto?> TryLoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var row = await _layoutRespo.GetByKeyAsync(PhotoWallLayoutDto.DefaultKey, cancellationToken);
            if (row == null || string.IsNullOrWhiteSpace(row.LayoutJson))
                return null;

            return JsonSerializer.Deserialize<PhotoWallLayoutDto>(row.LayoutJson, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load photo wall layout; using seed.");
            return null;
        }
    }

    public static PhotoWallLayoutDto SeedDefault(IReadOnlyList<PlantWallSeed> plants)
    {
        var ordered = plants
            .OrderByDescending(p => !string.IsNullOrWhiteSpace(p.CoverImagePath))
            .ThenByDescending(p => p.LatestVisualActivityAt ?? DateTime.MinValue)
            .ThenBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tiles = new List<PhotoWallTileDto>();
        for (var i = 0; i < ordered.Count; i++)
        {
            var colSpan = 1;
            var rowSpan = 1;
            if (i == 0 && ordered.Count > 0)
            {
                colSpan = 2;
                rowSpan = 2;
            }
            else if (i == 1)
            {
                colSpan = 2;
            }
            else if (i == 2)
            {
                rowSpan = 2;
            }

            tiles.Add(new PhotoWallTileDto
            {
                PlantId = ordered[i].Id,
                Order = i,
                ColSpan = colSpan,
                RowSpan = rowSpan,
                Zoom = 1,
                FocusX = 0,
                FocusY = 100
            });
        }

        var layout = new PhotoWallLayoutDto
        {
            Version = PhotoWallLayoutDto.CurrentVersion,
            Columns = PhotoWallLayoutDto.DefaultColumns,
            GapX = PhotoWallLayoutDto.DefaultGapPx,
            GapY = PhotoWallLayoutDto.DefaultGapPx,
            Tiles = tiles
        };
        var packed = PhotoWallPacker.Pack(tiles, layout.Columns);
        var rowCount = packed.Count == 0 ? 1 : packed.Max(t => t.Row + t.RowSpan);
        layout.ColGaps = PhotoWallPacker.NormalizeColGaps(layout);
        layout.RowGaps = PhotoWallPacker.NormalizeRowGaps(layout, rowCount);
        return layout;
    }

    public static PhotoWallLayoutDto Merge(PhotoWallLayoutDto stored, IReadOnlyList<PlantWallSeed> plants)
    {
        var byId = plants.ToDictionary(p => p.Id);
        var kept = stored.Tiles
            .Where(t => byId.ContainsKey(t.PlantId))
            .OrderBy(t => t.Order)
            .Select((t, index) => CopyTile(t, index))
            .ToList();

        var present = kept.Select(t => t.PlantId).ToHashSet();
        foreach (var plant in plants.Where(p => !present.Contains(p.Id)))
        {
            kept.Add(new PhotoWallTileDto
            {
                PlantId = plant.Id,
                Order = kept.Count,
                ColSpan = 1,
                RowSpan = 1,
                Zoom = 1,
                FocusX = 0,
                FocusY = 100
            });
        }

        var layout = new PhotoWallLayoutDto
        {
            Version = PhotoWallLayoutDto.CurrentVersion,
            Columns = ColumnsForTiles(stored.Columns, kept),
            GapX = ClampGap(stored.GapX),
            GapY = ClampGap(stored.GapY),
            Tiles = kept,
            ColGaps = stored.ColGaps,
            RowGaps = stored.RowGaps,
            Diagonals = stored.Diagonals
        };

        var packed = PhotoWallPacker.Pack(kept, layout.Columns);
        var rowCount = packed.Count == 0 ? 1 : packed.Max(t => t.Row + t.RowSpan);
        layout.ColGaps = PhotoWallPacker.NormalizeColGaps(layout);
        layout.RowGaps = PhotoWallPacker.NormalizeRowGaps(layout, rowCount);
        return layout;
    }

    private static PhotoWallLayoutDto Normalize(PhotoWallLayoutDto layout, IReadOnlySet<Guid> validPlantIds)
    {
        var tiles = layout.Tiles
            .Where(t => validPlantIds.Contains(t.PlantId))
            .GroupBy(t => t.PlantId)
            .Select(g => g.First())
            .OrderBy(t => t.Order)
            .Select((t, index) => CopyTile(t, index))
            .ToList();

        foreach (var id in validPlantIds.Where(id => tiles.All(t => t.PlantId != id)))
        {
            tiles.Add(new PhotoWallTileDto
            {
                PlantId = id,
                Order = tiles.Count,
                ColSpan = 1,
                RowSpan = 1,
                Zoom = 1,
                FocusX = 0,
                FocusY = 100
            });
        }

        var result = new PhotoWallLayoutDto
        {
            Version = PhotoWallLayoutDto.CurrentVersion,
            Columns = ColumnsForTiles(layout.Columns, tiles),
            GapX = ClampGap(layout.GapX),
            GapY = ClampGap(layout.GapY),
            Tiles = tiles,
            ColGaps = layout.ColGaps,
            RowGaps = layout.RowGaps
        };

        var packed = PhotoWallPacker.Pack(tiles, result.Columns);
        var rowCount = packed.Count == 0 ? 1 : packed.Max(t => t.Row + t.RowSpan);
        result.ColGaps = PhotoWallPacker.NormalizeColGaps(result);
        result.RowGaps = PhotoWallPacker.NormalizeRowGaps(result, rowCount);
        return result;
    }

    private static PhotoWallTileDto CopyTile(PhotoWallTileDto t, int order) => new()
    {
        PlantId = t.PlantId,
        Order = order,
        ColSpan = ClampSpan(t.ColSpan),
        RowSpan = ClampSpan(t.RowSpan),
        Zoom = ClampZoom(t.Zoom),
        FocusX = ClampFocus(t.FocusX),
        FocusY = ClampFocus(t.FocusY)
    };

    private static int ClampSpan(int value) => Math.Max(1, value);
    private static int ClampColumns(int value) => Math.Max(2, value <= 0 ? PhotoWallLayoutDto.DefaultColumns : value);
    private static int ClampGap(int value) => Math.Max(0, value);
    private static double ClampZoom(double value) => value <= 0 || double.IsNaN(value) || double.IsInfinity(value) ? 1 : value;
    private static double ClampFocus(double value) => double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 100);

    private static int ColumnsForTiles(int columns, IReadOnlyList<PhotoWallTileDto> tiles)
    {
        var maxSpan = tiles.Count == 0 ? 1 : tiles.Max(t => ClampSpan(t.ColSpan));
        return ClampColumns(Math.Max(columns, maxSpan));
    }
}

public sealed record PlantWallSeed(Guid Id, string DisplayName, string? CoverImagePath, DateTime? LatestVisualActivityAt);

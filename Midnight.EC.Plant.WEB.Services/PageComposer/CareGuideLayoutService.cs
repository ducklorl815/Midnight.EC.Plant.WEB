using System.Text.Json;
using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Models.Models;
using Midnight.EC.Plant.WEB.Models.Respository;
using Midnight.EC.Plant.WEB.Utility.Json;

namespace Midnight.EC.Plant.WEB.Services.PageComposer;

public class CareGuideLayoutService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonHelper.DefaultOptions)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly PageComposerLayoutRespo _layoutRespo;
    private readonly ILogger<CareGuideLayoutService> _logger;

    public CareGuideLayoutService(
        PageComposerLayoutRespo layoutRespo,
        ILogger<CareGuideLayoutService> logger)
    {
        _layoutRespo = layoutRespo;
        _logger = logger;
    }

    public async Task<CareGuideSectionOrderDto> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var row = await _layoutRespo.GetByKeyAsync(CareGuideSectionOrderDto.LayoutKey, cancellationToken);
            if (row == null || string.IsNullOrWhiteSpace(row.LayoutJson))
            {
                return CareGuideSectionOrderDto.Normalize(null);
            }

            var parsed = JsonSerializer.Deserialize<CareGuideSectionOrderDto>(row.LayoutJson, JsonOptions);
            return CareGuideSectionOrderDto.Normalize(parsed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Load care guide section order failed; using default.");
            return CareGuideSectionOrderDto.Normalize(null);
        }
    }

    public async Task SaveAsync(CareGuideSectionOrderDto order, CancellationToken cancellationToken = default)
    {
        var normalized = CareGuideSectionOrderDto.Normalize(order);
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        await _layoutRespo.UpsertAsync(new PageComposerLayoutModel
        {
            LayoutKey = CareGuideSectionOrderDto.LayoutKey,
            LayoutJson = json,
            ModifyDate = DateTime.UtcNow
        }, cancellationToken);
    }
}

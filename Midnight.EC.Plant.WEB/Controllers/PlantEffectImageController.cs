using Microsoft.AspNetCore.Mvc;
using Midnight.EC.Plant.WEB.Services.PlantEffect;

namespace Midnight.EC.Plant.WEB.Controllers;

public class PlantEffectImageController : Controller
{
    private readonly PlantEffectImageService _effectService;
    private readonly ILogger<PlantEffectImageController> _logger;

    public PlantEffectImageController(
        PlantEffectImageService effectService,
        ILogger<PlantEffectImageController> logger)
    {
        _effectService = effectService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Latest(Guid photoId, CancellationToken cancellationToken)
    {
        if (photoId == Guid.Empty)
            return BadRequest(new { error = "photoId 必填。" });

        var latest = await _effectService.GetLatestAsync(photoId, cancellationToken);
        if (latest == null)
            return NotFound(new { error = "尚無效果圖。" });

        return Json(latest);
    }

    [HttpPost]
    public async Task<IActionResult> Generate(Guid photoId, CancellationToken cancellationToken = default)
    {
        if (photoId == Guid.Empty)
            return BadRequest(new { error = "photoId 必填。" });

        try
        {
            var result = await _effectService.GenerateAsync(photoId, forceRegenerate: false, cancellationToken);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generate effect failed for photo {PhotoId}", photoId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Regenerate(Guid photoId, CancellationToken cancellationToken = default)
    {
        if (photoId == Guid.Empty)
            return BadRequest(new { error = "photoId 必填。" });

        try
        {
            var result = await _effectService.GenerateAsync(photoId, forceRegenerate: true, cancellationToken);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Regenerate effect failed for photo {PhotoId}", photoId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

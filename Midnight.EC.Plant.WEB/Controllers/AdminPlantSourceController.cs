using Microsoft.AspNetCore.Mvc;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Repositories;
using Midnight.EC.Plant.WEB.Services.Interfaces;
using Midnight.EC.Plant.WEB.ViewModels;

namespace Midnight.EC.Plant.WEB.Controllers;

[Route("Admin/[controller]")]
public class AdminPlantSourceController : Controller
{
    private readonly IPlantSourceService _plantSourceService;
    private readonly IPlantSpeciesRepository _speciesRepository;
    private readonly ILogger<AdminPlantSourceController> _logger;

    public AdminPlantSourceController(
        IPlantSourceService plantSourceService,
        IPlantSpeciesRepository speciesRepository,
        ILogger<AdminPlantSourceController> logger)
    {
        _plantSourceService = plantSourceService;
        _speciesRepository = speciesRepository;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var sources = await _plantSourceService.GetAllAsync(cancellationToken);
        var model = new AdminPlantSourceListViewModel
        {
            Sources = sources.Select(s => new AdminPlantSourceListItemViewModel
            {
                Id = s.Id,
                SpeciesName = s.SpeciesName,
                Title = s.Title,
                Url = s.Url,
                SourceType = s.SourceType,
                ReliabilityLevel = s.ReliabilityLevel,
                CreatedAt = s.CreatedAt
            }).ToList()
        };

        return View(model);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await BuildCreateViewModel(new AdminPlantSourceCreateViewModel(), cancellationToken));
    }

    [HttpPost("Parse")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Parse(AdminPlantSourceCreateViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Url))
        {
            ModelState.AddModelError(nameof(model.Url), "請輸入 URL。");
            return View("Create", await BuildCreateViewModel(model, cancellationToken));
        }

        try
        {
            var parsed = await _plantSourceService.ParseUrlAsync(model.Url, model.SourceType, cancellationToken);
            model.Preview = MapPreview(parsed);
            if (parsed.IsExisting)
            {
                TempData["Info"] = "此 URL 已存在於資料庫，以下為快取結果。";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parse URL failed: {Url}", model.Url);
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return View("Create", await BuildCreateViewModel(model, cancellationToken));
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminPlantSourceCreateViewModel model, CancellationToken cancellationToken)
    {
        if (model.SpeciesIds.Length == 0 || string.IsNullOrWhiteSpace(model.Url))
        {
            ModelState.AddModelError(string.Empty, "請先完成解析並選擇至少一個物種。");
            return View(await BuildCreateViewModel(model, cancellationToken));
        }

        try
        {
            var parsed = await _plantSourceService.ParseUrlAsync(model.Url, model.SourceType, cancellationToken);
            var saved = await _plantSourceService.SaveAsync(model.SpeciesIds, parsed, model.Title, cancellationToken);
            TempData["Success"] = parsed.IsExisting
                ? "此 URL 已存在，已追加關聯物種。"
                : "外部來源已儲存。";
            return RedirectToAction(nameof(Details), new { id = saved.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save plant source failed");
            ModelState.AddModelError(string.Empty, ex.Message);
            try
            {
                model.Preview = MapPreview(await _plantSourceService.ParseUrlAsync(model.Url, model.SourceType, cancellationToken));
            }
            catch
            {
                // keep existing preview if re-parse fails
            }

            return View(await BuildCreateViewModel(model, cancellationToken));
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var source = await _plantSourceService.GetByIdAsync(id, cancellationToken);
        if (source == null)
        {
            return NotFound();
        }

        var model = new AdminPlantSourceDetailViewModel
        {
            Id = source.Id,
            SpeciesName = source.SpeciesName,
            LinkedSpeciesNames = source.LinkedSpeciesNames,
            SourceType = source.SourceType,
            Title = source.Title,
            Url = source.Url,
            Domain = source.Domain,
            Author = source.Author,
            ReliabilityLevel = source.ReliabilityLevel,
            ContentHash = source.ContentHash,
            LatestContent = source.LatestContent == null ? null : new ParsedPreviewViewModel
            {
                Summary = source.LatestContent.Summary,
                Keywords = source.LatestContent.Keywords,
                CleanText = source.LatestContent.CleanText,
                ContentHash = source.LatestContent.ContentHash,
                ParserType = source.LatestContent.ParserType ?? string.Empty,
                Status = source.LatestContent.Status,
                ErrorMessage = source.LatestContent.ErrorMessage
            }
        };

        return View(model);
    }

    [HttpPost("{id:int}/Reparse")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reparse(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _plantSourceService.ReparseAsync(id, cancellationToken);
            TempData["Success"] = "已重新解析。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reparse failed for source {SourceId}", id);
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<AdminPlantSourceCreateViewModel> BuildCreateViewModel(AdminPlantSourceCreateViewModel model, CancellationToken cancellationToken)
    {
        var species = await _speciesRepository.GetAllAsync(cancellationToken);
        model.SpeciesOptions = species.Select(s => new SpeciesOptionViewModel
        {
            Id = s.Id,
            Name = FormatSpeciesOptionName(s)
        }).ToList();
        return model;
    }

    private static string FormatSpeciesOptionName(Midnight.EC.Plant.WEB.Models.Entities.PlantSpecies species)
    {
        var chinese = species.ChineseName ?? species.CommonName;
        if (!string.IsNullOrWhiteSpace(chinese) && !string.IsNullOrWhiteSpace(species.ScientificName))
        {
            return $"{chinese}（{species.ScientificName}）";
        }

        return chinese ?? species.ScientificName;
    }

    private static ParsedPreviewViewModel MapPreview(ParsedContentDto parsed) => new()
    {
        NormalizedUrl = parsed.NormalizedUrl,
        UrlHash = parsed.UrlHash,
        SourceType = parsed.SourceType,
        Title = parsed.Title,
        Author = parsed.Author,
        Domain = parsed.Domain,
        Summary = parsed.Summary,
        Keywords = parsed.Keywords,
        CleanText = parsed.CleanText,
        ContentHash = parsed.ContentHash,
        ParserType = parsed.ParserType,
        ParserVersion = parsed.ParserVersion,
        Status = parsed.Status,
        ErrorMessage = parsed.ErrorMessage,
        ReliabilityLevel = parsed.ReliabilityLevel,
        IsExisting = parsed.IsExisting,
        ExistingSourceId = parsed.ExistingSourceId,
        RawText = parsed.RawText
    };
}

using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.AI;

public class PlantAnalysisContext
{
    public PlantDto Plant { get; set; } = null!;
    public PlantKnowledgeDto? Knowledge { get; set; }
    public List<PlantDiaryDto> Diaries { get; set; } = [];
    public List<PlantImageDto> Images { get; set; } = [];
    public List<PlantAnalysisDto> PreviousAnalyses { get; set; } = [];
    public List<PlantSourceContentDto> Sources { get; set; } = [];
    public List<PlantCareRecordDto> CareRecords { get; set; } = [];
    public PlantProfileDto? Profile { get; set; }
    public PlantTrendDto? Trend { get; set; }
    public AnalysisScope Scope { get; set; } = AnalysisScope.Recent30Days;
    public Guid? FocusImageId { get; set; }
    public string? FocusImageNote { get; set; }
    public string? FocusImageAbsolutePath { get; set; }
    public string? FocusImageContentType { get; set; }
}

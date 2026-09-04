using System.ComponentModel.DataAnnotations;
using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.ViewModels;

public class PlantListViewModel
{
    public List<PlantDashboardCardViewModel> Plants { get; set; } = [];
    public List<PlantDashboardCardViewModel> OverdueWatering { get; set; } = [];
    public List<PlantDashboardCardViewModel> MissingWateringDate { get; set; } = [];
    public List<PlantDashboardCardViewModel> IncompleteData { get; set; } = [];
    public int TotalActiveReminders { get; set; }
    public int TotalOverdueReminders { get; set; }
}

public class PlantDashboardCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NickName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? SpeciesName { get; set; }
    public string? SpeciesChineseName { get; set; }
    public string? SpeciesScientificName { get; set; }
    public string? Location { get; set; }
    public string? CoverImagePath { get; set; }
    public int? LatestHealthScore { get; set; }
    public int ActiveReminderCount { get; set; }
    public int OverdueReminderCount { get; set; }
    public List<PlantReminderItemViewModel> TopReminders { get; set; } = [];
    public int? DaysSinceLastWatering { get; set; }
    public int WateringIntervalDays { get; set; } = 7;
    public bool IsWateringOverdue { get; set; }
    public bool MissingLastWateringDate { get; set; }
    public bool KnowledgeIncomplete { get; set; }
    public bool EnvironmentIncomplete { get; set; }
    public string? ActualLightLabel { get; set; }
}

public class PlantListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SpeciesName { get; set; }
    public string? Location { get; set; }
}

public class CreatePlantViewModel
{
    [Required(ErrorMessage = "請輸入暱稱")]
    [Display(Name = "暱稱")]
    public string NickName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入中文名")]
    [Display(Name = "中文名")]
    public string ChineseName { get; set; } = string.Empty;

    [Display(Name = "今天已澆水")]
    public bool WateredToday { get; set; } = true;

    [Display(Name = "位置備註")]
    public string? Location { get; set; }

    [Display(Name = "備註")]
    public string? Description { get; set; }

    [Display(Name = "幼苗時間")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }
}

public class ConfirmSpeciesViewModel
{
    public CreatePlantViewModel Draft { get; set; } = new();
    public List<SpeciesCandidateItemViewModel> Candidates { get; set; } = [];
    public int? SelectedIndex { get; set; }

    [Display(Name = "手填學名再查")]
    public string? ManualScientificName { get; set; }
}

public class SpeciesCandidateItemViewModel
{
    public string ScientificName { get; set; } = string.Empty;
    public string? CommonName { get; set; }
    public string? ChineseName { get; set; }
    public string? Genus { get; set; }
    public string? Family { get; set; }
    public string? ImageUrl { get; set; }
    public string? TaxonId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
}

public class EditPlantViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "請輸入植物名稱")]
    [Display(Name = "我的植物名稱")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "暱稱")]
    public string? NickName { get; set; }

    [Display(Name = "位置")]
    public string? Location { get; set; }

    [Display(Name = "備註")]
    public string? Description { get; set; }

    [Display(Name = "幼苗時間")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    public string? SpeciesName { get; set; }
    public string? SpeciesScientificName { get; set; }
}

public class PlantDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NickName { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? SpeciesName { get; set; }
    public string? SpeciesChineseName { get; set; }
    public string? SpeciesScientificName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public string? CoverImagePath { get; set; }
    public int? LatestHealthScore { get; set; }
    public PlantKnowledgeViewModel? Knowledge { get; set; }
    public List<PlantPhotoItemViewModel> Photos { get; set; } = [];
    public List<PlantAnalysisItemViewModel> Analyses { get; set; } = [];
    public List<PlantCareRecordItemViewModel> CareRecords { get; set; } = [];
    public PlantTrendViewModel Trend { get; set; } = new();
    public int? DaysSinceLastWatering { get; set; }
    public CreateCareRecordViewModel NewCareRecord { get; set; } = new();
    public CreatePhotoViewModel NewPhoto { get; set; } = new();
    public TodayLogViewModel TodayLog { get; set; } = new();
    public TodayLogViewModel BackfillLog { get; set; } = new();
    public PlantProfileViewModel Profile { get; set; } = new();
    public PlantCareSuggestionsViewModel CareSuggestions { get; set; } = new();
    public SyncKnowledgeViewModel SyncKnowledge { get; set; } = new();
    public List<PlantReminderItemViewModel> Reminders { get; set; } = [];
}

public class PlantPhotoItemViewModel
{
    public int Id { get; set; }
    public string PublicPath { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool IsCover { get; set; }
    public DateTime CreatedAt { get; set; }
    public PlantAnalysisItemViewModel? LatestAnalysis { get; set; }
}

public class CreatePhotoViewModel
{
    [Display(Name = "備註")]
    public string? Note { get; set; }

    [Display(Name = "設為封面")]
    public bool SetAsCover { get; set; }
}

public class UpdatePhotoNoteViewModel
{
    [Display(Name = "備註")]
    public string? Note { get; set; }
}

public class PlantKnowledgeViewModel
{
    public string? LightRequirement { get; set; }
    public string? WaterRequirement { get; set; }
    public string? HumidityRequirement { get; set; }
    public string? TemperatureRange { get; set; }
    public string? SoilRequirement { get; set; }
    public string? FertilizerRequirement { get; set; }
    public string? CareSummary { get; set; }
    public string? ExternalCareGuide { get; set; }
    public bool IsSparse { get; set; }
}

public class PlantCareSuggestionsViewModel
{
    public int? SuggestedWateringIntervalDays { get; set; }
    public decimal? SuggestedHumidityMin { get; set; }
    public decimal? SuggestedHumidityMax { get; set; }
    public decimal? SuggestedTemperatureMin { get; set; }
    public decimal? SuggestedTemperatureMax { get; set; }
    public string? AiWateringAdvice { get; set; }
    public string? AiGrowthTrend { get; set; }
    public string? PersonalCareNotesHint { get; set; }
}

public class SyncKnowledgeViewModel
{
    [Display(Name = "物種搜尋關鍵字")]
    public string SpeciesKeyword { get; set; } = string.Empty;
}

public class PlantDiaryItemViewModel
{
    public int Id { get; set; }
    public DateTime DiaryDate { get; set; }
    public string? Title { get; set; }
    public string? Note { get; set; }
    public List<PlantImageItemViewModel> Images { get; set; } = [];
}

public class PlantImageItemViewModel
{
    public int Id { get; set; }
    public string PublicPath { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
}

public class CreateDiaryViewModel
{
    [Display(Name = "日期")]
    [DataType(DataType.Date)]
    public DateTime DiaryDate { get; set; } = DateTime.Today;

    [Display(Name = "標題")]
    public string? Title { get; set; }

    [Display(Name = "日記內容")]
    public string? Note { get; set; }
}

public class PlantAnalysisItemViewModel
{
    public int Id { get; set; }
    public int? ImageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Summary { get; set; }
    public int? HealthScore { get; set; }
    public decimal? Confidence { get; set; }
    public string? ResultJson { get; set; }
    public bool IsExpanded { get; set; }
}

public class AnalysisStatusViewModel
{
    public int JobId { get; set; }
    public AnalysisJobStatus Status { get; set; }
    public int? AnalysisId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PlantCareRecordItemViewModel
{
    public int Id { get; set; }
    public DateTime RecordDate { get; set; }
    public CareRecordType CareType { get; set; }
    public decimal? NumericValue { get; set; }
    public string? Unit { get; set; }
    public string? Note { get; set; }
    public string DisplayValue { get; set; } = string.Empty;
}

public class CreateCareRecordViewModel
{
    [Display(Name = "日期")]
    [DataType(DataType.Date)]
    public DateTime RecordDate { get; set; } = DateTime.Today;

    [Display(Name = "類型")]
    public CareRecordType CareType { get; set; } = CareRecordType.Watering;

    [Display(Name = "數值")]
    public decimal? NumericValue { get; set; }

    [Display(Name = "單位")]
    public string? Unit { get; set; }

    [Display(Name = "備註")]
    public string? Note { get; set; }
}

public class TodayLogViewModel
{
    public bool Watered { get; set; }
    public bool Fertilized { get; set; }

    [Display(Name = "一句話")]
    public string? Note { get; set; }

    /// <summary>僅補記用；日常極簡列為今天。</summary>
    [DataType(DataType.Date)]
    [Display(Name = "補記日期")]
    public DateTime? LogDate { get; set; }
}

public class PlantTrendViewModel
{
    public string LabelsJson { get; set; } = "[]";
    public string HealthScoresJson { get; set; } = "[]";
    public string TemperaturesJson { get; set; } = "[]";
    public string HumiditiesJson { get; set; } = "[]";
    public string LightLevelsJson { get; set; } = "[]";
    public string WateringCountsJson { get; set; } = "[]";
    public DateTime? LastWateringDate { get; set; }
    public int TotalWateringCount { get; set; }
}

public class PlantProfileViewModel
{
    [Display(Name = "澆水週期（天）")]
    public int? WateringIntervalDays { get; set; }

    [Display(Name = "施肥週期（天）")]
    public int? FertilizingIntervalDays { get; set; }

    [Display(Name = "目標濕度下限 %")]
    public decimal? TargetHumidityMin { get; set; }

    [Display(Name = "目標濕度上限 %")]
    public decimal? TargetHumidityMax { get; set; }

    [Display(Name = "目標溫度下限 °C")]
    public decimal? TargetTemperatureMin { get; set; }

    [Display(Name = "目標溫度上限 °C")]
    public decimal? TargetTemperatureMax { get; set; }

    [Display(Name = "個人化照護備註")]
    public string? PersonalCareNotes { get; set; }

    [Display(Name = "實際位置類型")]
    public PlacementType? ActualPlacement { get; set; }

    [Display(Name = "實際日照")]
    public LightLevel? ActualLight { get; set; }

    [Display(Name = "是否有遮雨")]
    public bool? HasRainCover { get; set; }

    [Display(Name = "介質類型")]
    public string? SubstrateType { get; set; }

    [Display(Name = "所在縣市")]
    public string? City { get; set; }

    [Display(Name = "我知道環境不理想")]
    public bool AcknowledgeMismatch { get; set; }

    public List<string> MismatchWarnings { get; set; } = [];
    public LightLevel? SuggestedLight { get; set; }
    public List<string> CareTaboos { get; set; } = [];
    public string? SuggestedLightLabel => LightLevelDisplay.ToLabel(SuggestedLight);
    public bool EnvironmentIncomplete { get; set; }
}

public class PlantReminderItemViewModel
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public string? PlantName { get; set; }
    public ReminderType ReminderType { get; set; }
    public ReminderPriority Priority { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsOverdue { get; set; }
}

public class PlantTimelineItemViewModel
{
    public TimelineEventType EventType { get; set; }
    public DateTime EventDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int? HealthScore { get; set; }
}

public class PlantCompareViewModel
{
    public List<PlantCompareOptionViewModel> PlantOptions { get; set; } = [];
    public int[] SelectedPlantIds { get; set; } = [];
    public PlantComparisonResultViewModel? Result { get; set; }
}

public class PlantCompareOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SpeciesName { get; set; }
    public bool IsSelected { get; set; }
}

public class PlantComparisonResultViewModel
{
    public List<PlantComparisonRowViewModel> Items { get; set; } = [];
    public string HealthScoreLabelsJson { get; set; } = "[]";
    public string HealthScoresJson { get; set; } = "[]";
}

public class PlantComparisonRowViewModel
{
    public int PlantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SpeciesName { get; set; }
    public string? Location { get; set; }
    public int? LatestHealthScore { get; set; }
    public DateTime? LastWateringDate { get; set; }
    public int WateringCount30Days { get; set; }
    public decimal? AvgTemperature { get; set; }
    public decimal? AvgHumidity { get; set; }
    public string? GrowthTrend { get; set; }
    public int ActiveReminderCount { get; set; }
}

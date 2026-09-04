using Midnight.EC.Plant.WEB.Models.Enums;



namespace Midnight.EC.Plant.WEB.Models.DTOs;



public class PlantProfileDto

{

    public int Id { get; set; }

    public int PlantId { get; set; }

    public int? WateringIntervalDays { get; set; }

    public int? FertilizingIntervalDays { get; set; }

    public decimal? TargetHumidityMin { get; set; }

    public decimal? TargetHumidityMax { get; set; }

    public decimal? TargetTemperatureMin { get; set; }

    public decimal? TargetTemperatureMax { get; set; }

    public string? PersonalCareNotes { get; set; }
    public PlacementType? ActualPlacement { get; set; }
    public LightLevel? ActualLight { get; set; }
    public bool? HasRainCover { get; set; }
    public string? SubstrateType { get; set; }
    public string? City { get; set; }
    public LightLevel? OverrideSuggestedLight { get; set; }
    public string? OverrideCareTaboosJson { get; set; }
    public bool WateringIntervalDetachedFromWiki { get; set; }
    public bool EnvironmentMismatchAcknowledged { get; set; }

}



public class PlantReminderDto

{

    public int Id { get; set; }

    public int PlantId { get; set; }

    public string? PlantName { get; set; }

    public ReminderType ReminderType { get; set; }

    public ReminderPriority Priority { get; set; }

    public ReminderStatus Status { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Message { get; set; }

    public DateTime DueDate { get; set; }

    public bool IsOverdue { get; set; }

}



public class PlantTimelineEventDto

{

    public TimelineEventType EventType { get; set; }

    public DateTime EventDate { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public int? RelatedId { get; set; }

    public int? HealthScore { get; set; }

}



public class PlantComparisonItemDto

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



public class PlantComparisonResultDto

{

    public List<PlantComparisonItemDto> Items { get; set; } = [];

    public List<string> HealthScoreLabels { get; set; } = [];

    public List<int?> HealthScores { get; set; } = [];

}



public class PlantDashboardItemDto
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
    /// <summary>最近一張照片時間；無照片時為 null，照片牆排序用。</summary>
    public DateTime? LatestVisualActivityAt { get; set; }
    public int? LatestHealthScore { get; set; }
    public int ActiveReminderCount { get; set; }
    public int OverdueReminderCount { get; set; }
    public List<PlantReminderDto> TopReminders { get; set; } = [];
    public DateTime? LastWateringDate { get; set; }
    public int? DaysSinceLastWatering { get; set; }
    public int WateringIntervalDays { get; set; } = 7;
    public bool IsWateringOverdue { get; set; }
    public bool MissingLastWateringDate { get; set; }
    public bool KnowledgeIncomplete { get; set; }
    public bool EnvironmentIncomplete { get; set; }
    public string? ActualLightLabel { get; set; }
}


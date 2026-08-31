using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Entities;

public class PlantReminder
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public ReminderType ReminderType { get; set; }
    public ReminderPriority Priority { get; set; }
    public ReminderStatus Status { get; set; } = ReminderStatus.Active;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime DueDate { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public DateTime? DismissedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Plant Plant { get; set; } = null!;
}

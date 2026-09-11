using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class NotificationReminderItemDto
{
    public Guid ReminderId { get; set; }
    public Guid PlantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public ReminderType ReminderType { get; set; }
    public ReminderPriority Priority { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int OverdueDays { get; set; }
    public bool ShowCompleteWater { get; set; }
    public bool ShowCompleteFertilize { get; set; }
}

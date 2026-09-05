using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantReminderModel
{
    public Guid ID { get; set; }
    public Guid Id { get => ID; set => ID = value; }
    public int Seq { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime CreatedAt { get => CreateDate; set => CreateDate = value; }
    public DateTime ModifyDate { get; set; }
    public DateTime UpdatedAt { get => ModifyDate; set => ModifyDate = value; }
    public bool Enabled { get; set; } = true;
    public bool IsActive { get => Enabled; set => Enabled = value; }
    public bool Deleted { get; set; }
    public Guid PlantID { get; set; }
    public Guid PlantId { get => PlantID; set => PlantID = value; }
    public ReminderType ReminderType { get; set; }
    public ReminderPriority Priority { get; set; }
    public ReminderStatus Status { get; set; } = ReminderStatus.Active;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime DueDate { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public DateTime? DismissedAt { get; set; }
}

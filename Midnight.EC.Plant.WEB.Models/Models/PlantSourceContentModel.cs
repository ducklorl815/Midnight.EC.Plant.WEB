using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.Models;

public class PlantSourceContentModel
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
    public Guid SourceID { get; set; }
    public Guid SourceId { get => SourceID; set => SourceID = value; }
    public string? RawText { get; set; }
    public string? CleanText { get; set; }
    public string? Summary { get; set; }
    public string? Keywords { get; set; }
    public string? ParsedJson { get; set; }
    public string? ParserType { get; set; }
    public string? ParserVersion { get; set; }
    public string? ContentHash { get; set; }
    public SourceContentStatus Status { get; set; } = SourceContentStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? ParsedAt { get; set; }
}

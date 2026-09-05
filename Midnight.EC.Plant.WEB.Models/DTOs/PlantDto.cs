namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PlantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid SpeciesId { get; set; }
    public string? NickName { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? EnvironmentNote { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? StartDate { get; set; }
    public bool Enabled { get; set; }
    public bool IsActive { get => Enabled; set => Enabled = value; }
    public DateTime CreateDate { get; set; }
    public DateTime CreatedAt { get => CreateDate; set => CreateDate = value; }
    public DateTime ModifyDate { get; set; }
    public PlantSpeciesDto? Species { get; set; }
    public PlantKnowledgeDto? Knowledge { get; set; }
}

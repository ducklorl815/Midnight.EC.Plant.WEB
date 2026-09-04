namespace Midnight.EC.Plant.WEB.Models.External;

public class ExternalSpeciesResult
{
    public string ScientificName { get; set; } = string.Empty;
    public string? CommonName { get; set; }
    public string? ChineseName { get; set; }
    public string? Genus { get; set; }
    public string? Family { get; set; }
    public string? TaxonId { get; set; }
    public string? ImageUrl { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}

public class ExternalKnowledgeResult
{
    public string? LightRequirement { get; set; }
    public string? WaterRequirement { get; set; }
    public string? HumidityRequirement { get; set; }
    public decimal? TemperatureMin { get; set; }
    public decimal? TemperatureMax { get; set; }
    public string? SoilRequirement { get; set; }
    public string? FertilizerRequirement { get; set; }
    public string? GrowthSeason { get; set; }
    public string? CareSummary { get; set; }
    public string? ExternalCareGuide { get; set; }
    public string Provider { get; set; } = string.Empty;
}

public class ExternalPlantSearchResult
{
    public ExternalSpeciesResult? Species { get; set; }
    public ExternalKnowledgeResult? Knowledge { get; set; }
    public string? ResolvedScientificName { get; set; }
    public string? IdentificationSource { get; set; }
}

public static class ScientificNameNormalizer
{
    public static string Normalize(string? scientificName)
    {
        if (string.IsNullOrWhiteSpace(scientificName))
        {
            return string.Empty;
        }

        var collapsed = string.Join(' ', scientificName.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return collapsed.ToLowerInvariant();
    }
}

public class PlantIdentificationResult
{
    public string ScientificName { get; set; } = string.Empty;
    public string? CommonName { get; set; }
    public string? Genus { get; set; }
    public string? Family { get; set; }
    public string? TaxonId { get; set; }
    public double Confidence { get; set; }
    public string Provider { get; set; } = "iNaturalist";
}

public class ExternalKnowledgePartial
{
    public string? LightRequirement { get; set; }
    public string? WaterRequirement { get; set; }
    public string? HumidityRequirement { get; set; }
    public decimal? TemperatureMin { get; set; }
    public decimal? TemperatureMax { get; set; }
    public string? SoilRequirement { get; set; }
    public string? FertilizerRequirement { get; set; }
    public string? GrowthSeason { get; set; }
    public string? CareSummary { get; set; }
    public string? ExternalCareGuide { get; set; }
    public string Provider { get; set; } = string.Empty;
}
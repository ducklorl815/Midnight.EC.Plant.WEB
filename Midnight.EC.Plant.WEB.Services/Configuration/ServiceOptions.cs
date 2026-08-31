namespace Midnight.EC.Plant.WEB.Services.Configuration;

public class ExternalPlantApiOptions
{
    public const string SectionName = "ExternalPlantApi";

    public TrefleApiOptions Trefle { get; set; } = new();
    public INaturalistApiOptions INaturalist { get; set; } = new();
    public GbifApiOptions GBIF { get; set; } = new();
}

public class TrefleApiOptions
{
    public string BaseUrl { get; set; } = "https://trefle.io/api/v1/";
    public string ApiKey { get; set; } = string.Empty;
}

public class INaturalistApiOptions
{
    public string BaseUrl { get; set; } = "https://api.inaturalist.org/v1/";
}

public class GbifApiOptions
{
    public string BaseUrl { get; set; } = "https://api.gbif.org/v1/";
}

public class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = "OpenAI";
    public OpenAiOptions OpenAI { get; set; } = new();
}

public class OpenAiOptions
{
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
}

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Local";
    public string RootPath { get; set; } = "uploads/plants";
}

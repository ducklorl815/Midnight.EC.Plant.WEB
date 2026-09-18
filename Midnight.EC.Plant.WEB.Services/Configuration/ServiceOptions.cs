namespace Midnight.EC.Plant.WEB.Services.Configuration;

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
    /// <summary>Chat / Vision 模型（分析、照護知識、找種）。</summary>
    public string Model { get; set; } = "gpt-4o-mini";
    /// <summary>Image edits / generation 模型。</summary>
    public string ImageModel { get; set; } = "gpt-image-1";
    /// <summary>Landscape effect base (~16:9 feel). OpenAI allows 1536x1024.</summary>
    public string ImageSize { get; set; } = "1536x1024";
    public string ImageQuality { get; set; } = "medium";
}

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Local";
    public string RootPath { get; set; } = "uploads/plants";
}

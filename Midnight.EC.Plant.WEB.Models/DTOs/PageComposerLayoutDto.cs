using System.Text.Json.Serialization;
using Midnight.EC.Plant.WEB.Models.Enums;

namespace Midnight.EC.Plant.WEB.Models.DTOs;

public class PageComposerLayoutDto
{
    public const int CurrentVersion = 2;
    public const string DefaultKey = "home";

    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    [JsonPropertyName("modules")]
    public List<PageComposerModuleDto> Modules { get; set; } = [];

    /// <summary>Legacy v1 field; migrated into a Banner module then cleared.</summary>
    [JsonPropertyName("banner")]
    public PageComposerBannerDto? LegacyBanner { get; set; }

    /// <summary>Legacy v1 field; used only while migrating Banner position.</summary>
    [JsonPropertyName("shellOrder")]
    public string? LegacyShellOrder { get; set; }
}

/// <summary>Legacy v1 banner payload (migrated into PageModuleType.Banner).</summary>
public class PageComposerBannerDto
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("ctaText")]
    public string? CtaText { get; set; }

    [JsonPropertyName("ctaHref")]
    public string? CtaHref { get; set; }
}

public class PageComposerModuleDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public PageModuleType Type { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("images")]
    public List<PageComposerImageDto> Images { get; set; } = [];

    /// <summary>CSS theme hook, e.g. theme-moss. Reserved for per-module styling.</summary>
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "theme-default";

    [JsonPropertyName("items")]
    public List<PageComposerItemDto> Items { get; set; } = [];

    [JsonPropertyName("ctaText")]
    public string? CtaText { get; set; }

    [JsonPropertyName("ctaHref")]
    public string? CtaHref { get; set; }

    /// <summary>Per-plant image framing for PlantDetailWallCarousel (module-owned, not photo wall).</summary>
    [JsonPropertyName("slideFrames")]
    public List<PageComposerSlideFrameDto> SlideFrames { get; set; } = [];
}

public class PageComposerSlideFrameDto
{
    [JsonPropertyName("plantId")]
    public Guid PlantId { get; set; }

    /// <summary>置換後的效果圖（PlantEffectImage.ID）。</summary>
    [JsonPropertyName("effectImageId")]
    public Guid? EffectImageId { get; set; }

    /// <summary>效果圖公開路徑（如 /uploads/plants/.../effects/...），供首頁與編輯預覽直接使用。</summary>
    [JsonPropertyName("effectImageUrl")]
    public string? EffectImageUrl { get; set; }

    /// <summary>字卡左上角 X 百分比（以圖片框為基準）。</summary>
    [JsonPropertyName("cardX")]
    public double CardX { get; set; } = 6;

    /// <summary>字卡左上角 Y 百分比（以圖片框為基準）。</summary>
    [JsonPropertyName("cardY")]
    public double CardY { get; set; } = 22;
}

public class PageComposerImageDto
{
    /// <summary>upload | plant | url（舊資料無欄位時視為 url／有路徑即顯示）</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "url";

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("alt")]
    public string? Alt { get; set; }

    [JsonPropertyName("plantId")]
    public Guid? PlantId { get; set; }

    [JsonPropertyName("imageId")]
    public Guid? ImageId { get; set; }

    /// <summary>顯示解析時：植栽引用失效。不必持久化語意，可隨 Build 寫入給編輯器。</summary>
    [JsonPropertyName("missing")]
    public bool Missing { get; set; }
}

/// <summary>拼圖 Banner 選圖：依盆分組的植栽照片目錄。</summary>
public class PageComposerPhotoCatalogPlantDto
{
    [JsonPropertyName("plantId")]
    public Guid PlantId { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("photos")]
    public List<PageComposerPhotoCatalogImageDto> Photos { get; set; } = [];
}

public class PageComposerPhotoCatalogImageDto
{
    [JsonPropertyName("imageId")]
    public Guid ImageId { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("isCover")]
    public bool IsCover { get; set; }
}

public class PageComposerItemDto
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }
}

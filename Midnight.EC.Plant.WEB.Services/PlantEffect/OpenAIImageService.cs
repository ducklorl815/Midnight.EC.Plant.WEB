using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Services.Configuration;
using Midnight.EC.Plant.WEB.Utility.Hash;
using Midnight.EC.Plant.WEB.Utility.Imaging;

namespace Midnight.EC.Plant.WEB.Services.PlantEffect;

public interface IOpenAIImageService
{
    Task<OpenAIImageEditResult> EditFromFileAsync(
        string absoluteImagePath,
        string prompt,
        CancellationToken cancellationToken = default);
}

public sealed class OpenAIImageEditResult
{
    public byte[] ImageBytes { get; set; } = [];
    public string ContentType { get; set; } = "image/png";
    public string? RequestId { get; set; }

    /// <summary>Actual model value sent in the multipart request.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Requested size field sent to OpenAI (e.g. 1536x1024).</summary>
    public string RequestedSize { get; set; } = string.Empty;

    /// <summary>Quality field sent to OpenAI.</summary>
    public string Quality { get; set; } = string.Empty;

    /// <summary>input_fidelity value if sent; null when omitted.</summary>
    public string? InputFidelity { get; set; }

    /// <summary>SHA-256 hex of the prompt text actually sent.</summary>
    public string PromptHash { get; set; } = string.Empty;

    /// <summary>Decoded output image width, when readable from binary header.</summary>
    public int? OutputWidth { get; set; }

    /// <summary>Decoded output image height, when readable from binary header.</summary>
    public int? OutputHeight { get; set; }
}

public class OpenAIImageService : IOpenAIImageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAIImageService> _logger;

    public OpenAIImageService(
        IHttpClientFactory httpClientFactory,
        IOptions<AiOptions> options,
        ILogger<OpenAIImageService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OpenAIImageEditResult> EditFromFileAsync(
        string absoluteImagePath,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey))
            throw new InvalidOperationException("尚未設定 AI:OpenAI:ApiKey。");

        if (!File.Exists(absoluteImagePath))
            throw new FileNotFoundException("找不到原始植物照片。", absoluteImagePath);

        var model = string.IsNullOrWhiteSpace(_options.OpenAI.ImageModel)
            ? "gpt-image-1"
            : _options.OpenAI.ImageModel.Trim();
        var size = string.IsNullOrWhiteSpace(_options.OpenAI.ImageSize)
            ? "1536x1024"
            : _options.OpenAI.ImageSize.Trim();
        var quality = string.IsNullOrWhiteSpace(_options.OpenAI.ImageQuality)
            ? "medium"
            : _options.OpenAI.ImageQuality.Trim();
        string? inputFidelity = model.StartsWith("gpt-image-1", StringComparison.OrdinalIgnoreCase)
            ? "high"
            : null;
        var promptHash = HashHelper.ComputeSha256(prompt);

        _logger.LogInformation(
            "OpenAI image edit request audit: endpoint={Endpoint} model={Model} size={Size} quality={Quality} input_fidelity={InputFidelity} promptHash={PromptHash} sourceFile={SourceFile}",
            "images/edits",
            model,
            size,
            quality,
            inputFidelity ?? "(omitted)",
            promptHash,
            Path.GetFileName(absoluteImagePath));

        var client = _httpClientFactory.CreateClient("OpenAI");

        await using var fileStream = File.OpenRead(absoluteImagePath);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GuessMime(absoluteImagePath));
        content.Add(fileContent, "image", Path.GetFileName(absoluteImagePath));
        content.Add(new StringContent(prompt), "prompt");
        content.Add(new StringContent(model), "model");
        content.Add(new StringContent(size), "size");
        content.Add(new StringContent(quality), "quality");
        if (inputFidelity != null)
            content.Add(new StringContent(inputFidelity), "input_fidelity");

        using var request = new HttpRequestMessage(HttpMethod.Post, "images/edits")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAI.ApiKey.Trim());

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var requestId = response.Headers.TryGetValues("x-request-id", out var ids)
            ? ids.FirstOrDefault()
            : null;

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "OpenAI image edit failed: status={Status} requestId={RequestId} model={Model} size={Size} quality={Quality} input_fidelity={InputFidelity} promptHash={PromptHash} body={Body}",
                (int)response.StatusCode,
                requestId,
                model,
                size,
                quality,
                inputFidelity ?? "(omitted)",
                promptHash,
                Truncate(body, 500));
            throw new InvalidOperationException($"OpenAI Image 失敗（HTTP {(int)response.StatusCode}）：{Truncate(body, 240)}");
        }

        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        if (data.GetArrayLength() == 0)
            throw new InvalidOperationException("OpenAI Image 未回傳圖片。");

        var first = data[0];
        byte[] bytes;
        var contentType = "image/png";

        if (first.TryGetProperty("b64_json", out var b64) && b64.ValueKind == JsonValueKind.String)
        {
            bytes = Convert.FromBase64String(b64.GetString()!);
        }
        else if (first.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
        {
            var url = urlProp.GetString()!;
            bytes = await client.GetByteArrayAsync(url, cancellationToken);
            contentType = GuessMime(url);
        }
        else
        {
            throw new InvalidOperationException("OpenAI Image 回應缺少 b64_json / url。");
        }

        int? outputWidth = null;
        int? outputHeight = null;
        if (ImageBinaryInfo.TryGetDimensions(bytes, out var w, out var h))
        {
            outputWidth = w;
            outputHeight = h;
        }

        _logger.LogInformation(
            "OpenAI image edit response audit: requestId={RequestId} model={Model} requestedSize={RequestedSize} quality={Quality} input_fidelity={InputFidelity} promptHash={PromptHash} outputWidth={OutputWidth} outputHeight={OutputHeight} byteLength={ByteLength} contentType={ContentType}",
            requestId,
            model,
            size,
            quality,
            inputFidelity ?? "(omitted)",
            promptHash,
            outputWidth,
            outputHeight,
            bytes.Length,
            contentType);

        return new OpenAIImageEditResult
        {
            ImageBytes = bytes,
            ContentType = contentType,
            RequestId = requestId,
            Model = model,
            RequestedSize = size,
            Quality = quality,
            InputFidelity = inputFidelity,
            PromptHash = promptHash,
            OutputWidth = outputWidth,
            OutputHeight = outputHeight
        };
    }

    private static string GuessMime(string pathOrUrl)
    {
        var ext = Path.GetExtension(pathOrUrl).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/png"
        };
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= max ? value : value[..max] + "…";
    }
}

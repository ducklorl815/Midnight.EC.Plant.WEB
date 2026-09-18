using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Midnight.EC.Plant.WEB.Models.External;

namespace Midnight.EC.Plant.WEB.Services.AI;

/// <summary>兩段式找種的第一段：只解析 1～3 候選，不寫滿照護知識。</summary>
public class OpenAISpeciesFinderService
{
    public const string PromptVersion = "species-find-v1";

    private readonly IChatCompletionEnvelope _chat;
    private readonly ILogger<OpenAISpeciesFinderService> _logger;

    public OpenAISpeciesFinderService(
        IChatCompletionEnvelope chat,
        ILogger<OpenAISpeciesFinderService> logger)
    {
        _chat = chat;
        _logger = logger;
    }

    public Task<IReadOnlyList<ExternalSpeciesResult>> FindByTextAsync(
        string query,
        CancellationToken cancellationToken = default) =>
        FindCoreAsync(query?.Trim() ?? string.Empty, null, null, cancellationToken);

    public async Task<IReadOnlyList<ExternalSpeciesResult>> FindByImageAsync(
        Stream imageStream,
        string fileName,
        string? chineseHint,
        CancellationToken cancellationToken = default)
    {
        await using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();
        if (bytes.Length == 0)
        {
            throw new InvalidOperationException("照片是空的，請換一張再試。");
        }

        var mime = GuessMime(fileName);
        return await FindCoreAsync(chineseHint?.Trim() ?? string.Empty, bytes, mime, cancellationToken);
    }

    private async Task<IReadOnlyList<ExternalSpeciesResult>> FindCoreAsync(
        string query,
        byte[]? imageBytes,
        string? mime,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) && imageBytes == null)
        {
            throw new InvalidOperationException("請輸入中文名／學名，或上傳照片。");
        }

        var isChinese = CareGuideJson.HasCjk(query);
        var userText = new StringBuilder();
        if (imageBytes != null)
        {
            userText.AppendLine("請依照片辨識最可能的植物物種（以栽培常見種為主）。");
            if (!string.IsNullOrWhiteSpace(query))
            {
                userText.AppendLine($"使用者提示名稱：{query}");
            }
        }
        else if (isChinese)
        {
            userText.AppendLine($"使用者輸入的中文名（信任中文名，勿改成其他主名）：{query}");
            userText.AppendLine("請找出最可能對應的拉丁學名候選（台灣常見栽培／流通名優先）。");
        }
        else
        {
            userText.AppendLine($"使用者輸入的學名或關鍵字：{query}");
            userText.AppendLine("請核對並給出最可能的接受學名候選。");
        }

        userText.AppendLine("回傳 JSON：{ \"candidates\": [ { \"scientificName\", \"commonName\", \"genus\", \"family\", \"alsoKnownAs\": [], \"identificationHint\" } ] }");
        userText.AppendLine("最多 3 筆；identificationHint 用一句繁中說明辨識重點或易混種差異。");
        userText.AppendLine("若無法辨識，candidates 給空陣列。");

        ChatCompletionMessage userMessage;
        if (imageBytes != null)
        {
            var b64 = Convert.ToBase64String(imageBytes);
            userMessage = new ChatCompletionMessage
            {
                Role = "user",
                Parts =
                [
                    ChatContentPart.FromText(userText.ToString()),
                    ChatContentPart.FromImageDataUrl($"data:{mime};base64,{b64}")
                ]
            };
        }
        else
        {
            userMessage = new ChatCompletionMessage { Role = "user", Text = userText.ToString() };
        }

        try
        {
            var content = await _chat.CompleteAsync(
                [
                    new ChatCompletionMessage
                    {
                        Role = "system",
                        Text = """
                            你是植物分類助理，服務台灣繁體中文使用者。
                            只做物種候選解析，不要寫栽培長文。
                            欄位名英文；identificationHint、alsoKnownAs 用繁體中文。
                            scientificName 必須是拉丁學名。不要捏造不存在的學名。
                            """
                    },
                    userMessage
                ],
                cancellationToken: cancellationToken);

            return ParseCandidates(content, isChinese ? query : null);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Species find failed for {Query}", query);
            throw new InvalidOperationException($"OpenAI 找種例外：{ex.GetType().Name}");
        }
    }

    private static List<ExternalSpeciesResult> ParseCandidates(string content, string? trustedChinese)
    {
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        if (!TryGet(root, "candidates", out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<ExternalSpeciesResult>();
        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var scientific = GetString(item, "scientificName");
            if (string.IsNullOrWhiteSpace(scientific)) continue;

            var also = new List<string>();
            if (TryGet(item, "alsoKnownAs", out var aka) && aka.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in aka.EnumerateArray())
                {
                    var s = a.ValueKind == JsonValueKind.String ? a.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(s)) also.Add(s.Trim());
                }
            }

            var hint = GetString(item, "identificationHint");
            if (also.Count > 0)
            {
                var akaText = "又名：" + string.Join("、", also.Take(4));
                hint = string.IsNullOrWhiteSpace(hint) ? akaText : $"{hint}（{akaText}）";
            }

            list.Add(new ExternalSpeciesResult
            {
                ScientificName = scientific.Trim(),
                CommonName = GetString(item, "commonName"),
                ChineseName = trustedChinese,
                Genus = GetString(item, "genus"),
                Family = GetString(item, "family"),
                IdentificationHint = hint,
                Provider = $"AI-{PromptVersion}",
                SourceType = "OpenAI",
                SourceId = scientific.Trim()
            });

            if (list.Count >= 3) break;
        }

        return list;
    }

    private static string GuessMime(string? fileName)
    {
        var ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
    }

    private static string? GetString(JsonElement root, string name)
    {
        if (!TryGet(root, name, out var el)) return null;
        return el.ValueKind == JsonValueKind.String ? el.GetString()?.Trim() : el.ToString();
    }

    private static bool TryGet(JsonElement root, string name, out JsonElement value)
    {
        if (root.TryGetProperty(name, out value)) return true;
        foreach (var p in root.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? string.Empty : s.Length <= max ? s : s[..max] + "…";
}

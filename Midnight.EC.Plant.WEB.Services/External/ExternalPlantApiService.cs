using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.External;
using Midnight.EC.Plant.WEB.Services.Configuration;
using Midnight.EC.Plant.WEB.Services.Interfaces;

namespace Midnight.EC.Plant.WEB.Services.External;

public class ExternalPlantApiService : IExternalPlantApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExternalPlantApiOptions _options;
    private readonly ILogger<ExternalPlantApiService> _logger;

    public ExternalPlantApiService(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalPlantApiOptions> options,
        ILogger<ExternalPlantApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PlantIdentificationResult?> IdentifyFromImageAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ExternalPlantApi");
            using var content = new MultipartFormDataContent();
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(imageContent, "image", fileName);

            var url = $"{_options.INaturalist.BaseUrl.TrimEnd('/')}/computervision/score_image";
            using var response = await client.PostAsync(url, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("iNaturalist vision failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                return null;
            }

            JsonElement? bestTaxon = null;
            double bestScore = 0;
            foreach (var item in results.EnumerateArray())
            {
                if (!item.TryGetProperty("taxon", out var candidateTaxon))
                {
                    continue;
                }

                if (!IsPlantTaxon(candidateTaxon))
                {
                    continue;
                }

                var score = item.TryGetProperty("combined_score", out var scoreEl) && scoreEl.TryGetDouble(out var s)
                    ? s
                    : 0;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTaxon = candidateTaxon;
                }
            }

            if (bestTaxon == null || bestScore < 0.15)
            {
                _logger.LogWarning("iNaturalist vision: no confident plant taxon (best={Score})", bestScore);
                return null;
            }

            var matchedTaxon = bestTaxon.Value;
            var (genus, family) = ExtractINaturalistTaxonomy(matchedTaxon);
            var rank = matchedTaxon.TryGetProperty("rank", out var rankEl) ? rankEl.GetString() : null;
            var name = matchedTaxon.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;

            return new PlantIdentificationResult
            {
                ScientificName = name,
                CommonName = matchedTaxon.TryGetProperty("preferred_common_name", out var cn) ? cn.GetString() : null,
                Genus = genus,
                Family = family,
                TaxonId = matchedTaxon.TryGetProperty("id", out var id) ? id.GetRawText() : null,
                Confidence = bestScore,
                Provider = rank == "species" ? "iNaturalist-Vision" : "iNaturalist-Vision-Genus"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iNaturalist vision identify error");
            return null;
        }
    }

    public async Task<ExternalPlantSearchResult?> SearchSpeciesAsync(string keyword, CancellationToken cancellationToken = default)
    {
        ExternalSpeciesResult? species = null;
        foreach (var term in ChineseKeywordExpander.Expand(keyword))
        {
            species = await ResolveSpeciesAsync(term, keyword, cancellationToken);
            if (species != null)
            {
                break;
            }
        }

        if (species == null)
        {
            return null;
        }

        var knowledgePartials = new List<ExternalKnowledgePartial>();
        var enrichTasks = new List<Task<ExternalKnowledgePartial?>>
        {
            FetchTrefleKnowledgeByNameAsync(species.ScientificName, cancellationToken),
            FetchGbifDistributionPartialAsync(species.ScientificName, cancellationToken),
            FetchWikipediaSummaryAsync(species, keyword, cancellationToken),
            FetchINaturalistTaxonPartialAsync(species.ScientificName, cancellationToken)
        };

        var enrichResults = await Task.WhenAll(enrichTasks);
        foreach (var partial in enrichResults)
        {
            if (partial != null)
            {
                knowledgePartials.Add(partial);
            }
        }

        var genusTemplate = GenusCareTemplateProvider.TryGet(species.Genus, species.Family)
            ?? GenusCareTemplateProvider.TryGetFromKeyword(keyword);
        if (genusTemplate != null)
        {
            knowledgePartials.Add(genusTemplate);
        }

        var mergedKnowledge = ExternalKnowledgeMerger.Merge(knowledgePartials.ToArray());
        if (string.IsNullOrWhiteSpace(mergedKnowledge.CareSummary))
        {
            mergedKnowledge.CareSummary = $"學名 {species.ScientificName}。";
        }

        return new ExternalPlantSearchResult
        {
            Species = species,
            Knowledge = mergedKnowledge,
            ResolvedScientificName = species.ScientificName,
            IdentificationSource = species.Provider
        };
    }

    public async Task<IReadOnlyList<ExternalSpeciesResult>> SearchSpeciesCandidatesAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        var bag = new Dictionary<string, ExternalSpeciesResult>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        foreach (var term in ChineseKeywordExpander.Expand(keyword))
        {
            await CollectINaturalistCandidatesAsync(term, keyword, bag, cancellationToken);
            await CollectGbifCandidatesAsync(term, keyword, bag, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Trefle.ApiKey) && bag.Count < 3)
            {
                var trefle = await SearchTrefleAsync(term, cancellationToken);
                if (trefle?.Species != null)
                {
                    TryAddCandidate(bag, trefle.Species, keyword);
                }
            }

            if (bag.Count >= 3)
            {
                break;
            }
        }

        return bag.Values.Take(3).ToList();
    }

    private async Task CollectINaturalistCandidatesAsync(
        string searchTerm,
        string originalKeyword,
        Dictionary<string, ExternalSpeciesResult> bag,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ExternalPlantApi");
            var url = $"{_options.INaturalist.BaseUrl.TrimEnd('/')}/taxa?q={Uri.EscapeDataString(searchTerm)}&iconic_taxa=Plantae&per_page=20";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("results", out var results))
            {
                return;
            }

            var ranked = results.EnumerateArray()
                .Select(item => (Item: item, Score: ScoreINaturalistCandidate(searchTerm, item)))
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .Take(8);

            foreach (var (item, _) in ranked)
            {
                if (bag.Count >= 3)
                {
                    break;
                }

                var (genus, family) = ExtractINaturalistTaxonomy(item);
                var imageUrl = item.TryGetProperty("default_photo", out var photo) && photo.ValueKind == JsonValueKind.Object
                    ? (photo.TryGetProperty("medium_url", out var mid) ? mid.GetString() : photo.TryGetProperty("square_url", out var sq) ? sq.GetString() : null)
                    : null;

                var species = new ExternalSpeciesResult
                {
                    ScientificName = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? searchTerm : searchTerm,
                    CommonName = item.TryGetProperty("preferred_common_name", out var cn) ? cn.GetString() : null,
                    ChineseName = ContainsCjk(originalKeyword) ? originalKeyword : null,
                    Genus = genus,
                    Family = family,
                    TaxonId = item.TryGetProperty("id", out var id) ? id.GetRawText() : null,
                    ImageUrl = imageUrl,
                    SourceType = "iNaturalist",
                    SourceId = item.TryGetProperty("id", out var sid) ? sid.GetRawText() : string.Empty,
                    Provider = "iNaturalist"
                };

                TryAddCandidate(bag, species, originalKeyword);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "iNaturalist candidate collect failed for {Term}", searchTerm);
        }
    }

    private async Task CollectGbifCandidatesAsync(
        string searchTerm,
        string originalKeyword,
        Dictionary<string, ExternalSpeciesResult> bag,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ExternalPlantApi");
            var url = $"{_options.GBIF.BaseUrl.TrimEnd('/')}/species/search?q={Uri.EscapeDataString(searchTerm)}&limit=10&status=ACCEPTED";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // fallback: match single
                var matched = await SearchGbifAsync(searchTerm, cancellationToken);
                if (matched?.Species != null)
                {
                    TryAddCandidate(bag, matched.Species, originalKeyword);
                }

                return;
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("results", out var results))
            {
                return;
            }

            foreach (var item in results.EnumerateArray())
            {
                if (bag.Count >= 3)
                {
                    break;
                }

                var scientific = item.TryGetProperty("scientificName", out var sn) ? sn.GetString()
                    : item.TryGetProperty("canonicalName", out var can) ? can.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(scientific))
                {
                    continue;
                }

                var kingdom = item.TryGetProperty("kingdom", out var k) ? k.GetString() : null;
                if (!string.IsNullOrWhiteSpace(kingdom) &&
                    !string.Equals(kingdom, "Plantae", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var species = new ExternalSpeciesResult
                {
                    ScientificName = scientific,
                    CommonName = item.TryGetProperty("canonicalName", out var cn) ? cn.GetString() : null,
                    ChineseName = ContainsCjk(originalKeyword) ? originalKeyword : null,
                    Genus = item.TryGetProperty("genus", out var genus) ? genus.GetString() : null,
                    Family = item.TryGetProperty("family", out var family) ? family.GetString() : null,
                    TaxonId = item.TryGetProperty("key", out var id) ? id.GetRawText()
                        : item.TryGetProperty("nubKey", out var nub) ? nub.GetRawText() : null,
                    SourceType = "GBIF",
                    SourceId = item.TryGetProperty("key", out var sid) ? sid.GetRawText() : string.Empty,
                    Provider = "GBIF"
                };

                TryAddCandidate(bag, species, originalKeyword);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GBIF candidate collect failed for {Term}", searchTerm);
        }
    }

    private static void TryAddCandidate(
        Dictionary<string, ExternalSpeciesResult> bag,
        ExternalSpeciesResult species,
        string originalKeyword)
    {
        var key = ScientificNameNormalizer.Normalize(species.ScientificName);
        if (string.IsNullOrWhiteSpace(key) || bag.ContainsKey(key) || bag.Count >= 3)
        {
            return;
        }

        if (ContainsCjk(originalKeyword))
        {
            species.ChineseName ??= originalKeyword;
        }

        // Prefer keeping iNaturalist when duplicate arrives later from GBIF (already guarded by ContainsKey).
        bag[key] = species;
    }

    private async Task<ExternalSpeciesResult?> ResolveSpeciesAsync(string searchTerm, string originalKeyword, CancellationToken cancellationToken)
    {
        var trefle = await SearchTrefleAsync(searchTerm, cancellationToken);
        if (trefle?.Species != null)
        {
            if (ContainsCjk(originalKeyword))
            {
                trefle.Species.ChineseName ??= originalKeyword;
            }

            return trefle.Species;
        }

        var inat = await SearchINaturalistAsync(searchTerm, cancellationToken);
        if (inat?.Species != null && IsAcceptableTaxonMatch(searchTerm, inat.Species))
        {
            if (ContainsCjk(originalKeyword))
            {
                inat.Species.ChineseName ??= originalKeyword;
            }

            return inat.Species;
        }

        var gbif = await SearchGbifAsync(searchTerm, cancellationToken);
        if (gbif?.Species != null)
        {
            if (ContainsCjk(originalKeyword))
            {
                gbif.Species.ChineseName ??= originalKeyword;
            }

            return gbif.Species;
        }

        return null;
    }

    private async Task<ExternalKnowledgePartial?> FetchTrefleKnowledgeByNameAsync(string scientificName, CancellationToken cancellationToken)
    {
        var trefle = await SearchTrefleAsync(scientificName, cancellationToken);
        if (trefle?.Species == null)
        {
            return null;
        }

        var partials = new List<ExternalKnowledgePartial>();
        if (trefle.Knowledge != null)
        {
            partials.Add(ExternalKnowledgeMerger.ToPartial(trefle.Knowledge));
        }

        var detail = await FetchTrefleSpeciesDetailAsync(trefle.Species, cancellationToken);
        if (detail != null)
        {
            partials.Add(detail);
        }

        return partials.Count == 0
            ? null
            : ExternalKnowledgeMerger.ToPartial(ExternalKnowledgeMerger.Merge(partials.ToArray()));
    }

    private async Task<ExternalKnowledgePartial?> FetchGbifDistributionPartialAsync(string scientificName, CancellationToken cancellationToken)
    {
        var gbif = await SearchGbifAsync(scientificName, cancellationToken);
        if (gbif?.Species == null)
        {
            return null;
        }

        var partial = ExternalKnowledgeMerger.ToPartial(gbif.Knowledge ?? new ExternalKnowledgeResult());
        try
        {
            if (string.IsNullOrWhiteSpace(gbif.Species.TaxonId))
            {
                return partial;
            }

            var client = _httpClientFactory.CreateClient("ExternalPlantApi");
            var url = $"{_options.GBIF.BaseUrl.TrimEnd('/')}/species/{gbif.Species.TaxonId}/distributions";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return partial;
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("results", out var results))
            {
                return partial;
            }

            var countries = results.EnumerateArray()
                .Select(r => r.TryGetProperty("location", out var loc) ? loc.GetString() : null)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .Take(8)
                .ToList();

            if (countries.Count > 0)
            {
                var dist = $"原生/分布參考（GBIF）：{string.Join("、", countries)}。";
                partial.CareSummary = string.IsNullOrWhiteSpace(partial.CareSummary) ? dist : $"{partial.CareSummary} {dist}";
            }

            partial.Provider = "GBIF";
            return partial;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GBIF distribution fetch failed for {Name}", scientificName);
            return partial;
        }
    }

    private async Task<ExternalKnowledgePartial?> FetchINaturalistTaxonPartialAsync(string scientificName, CancellationToken cancellationToken)
    {
        var inat = await SearchINaturalistAsync(scientificName, cancellationToken);
        return inat?.Knowledge == null ? null : ExternalKnowledgeMerger.ToPartial(inat.Knowledge);
    }

    private static IEnumerable<string> ExpandKeywords(string keyword) => ChineseKeywordExpander.Expand(keyword);

    private async Task<ExternalKnowledgePartial?> FetchTrefleSpeciesDetailAsync(ExternalSpeciesResult species, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Trefle.ApiKey))
        {
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("ExternalPlantApi");
            var slug = species.SourceType == "Trefle" && !string.IsNullOrWhiteSpace(species.SourceId)
                ? await ResolveTrefleSlugAsync(species, client, cancellationToken)
                : null;

            if (string.IsNullOrWhiteSpace(slug))
            {
                return null;
            }

            var url = $"{_options.Trefle.BaseUrl.TrimEnd('/')}/species/{slug}?token={_options.Trefle.ApiKey}";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("data", out var data))
            {
                return null;
            }

            return MapTrefleSpeciesDetail(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trefle species detail failed for {ScientificName}", species.ScientificName);
            return null;
        }
    }

    private async Task<string?> ResolveTrefleSlugAsync(ExternalSpeciesResult species, HttpClient client, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(species.ScientificName))
        {
            var slugGuess = species.ScientificName.ToLowerInvariant().Replace(' ', '-');
            return slugGuess;
        }

        return null;
    }

    private static ExternalKnowledgePartial MapTrefleSpeciesDetail(JsonElement data)
    {
        var partial = new ExternalKnowledgePartial { Provider = "TrefleDetail" };

        if (data.TryGetProperty("growth", out var growth))
        {
            if (growth.TryGetProperty("light", out var light) && light.TryGetInt32(out var lightVal))
            {
                partial.LightRequirement = DescribeLightScale(lightVal);
            }

            if (growth.TryGetProperty("soil_humidity", out var soilHumidity) && soilHumidity.TryGetInt32(out var soilHumidityVal))
            {
                partial.WaterRequirement = DescribeWaterScale(soilHumidityVal);
            }

            if (growth.TryGetProperty("atmospheric_humidity", out var humidity) && humidity.TryGetInt32(out var humidityVal))
            {
                partial.HumidityRequirement = DescribeHumidityScale(humidityVal);
            }
        }

        if (data.TryGetProperty("specifications", out var specs))
        {
            if (specs.TryGetProperty("temperature_minimum", out var tMin) &&
                tMin.TryGetProperty("deg_c", out var tMinC) && tMinC.TryGetDecimal(out var minTemp))
            {
                partial.TemperatureMin = minTemp;
            }

            if (specs.TryGetProperty("temperature_maximum", out var tMax) &&
                tMax.TryGetProperty("deg_c", out var tMaxC) && tMaxC.TryGetDecimal(out var maxTemp))
            {
                partial.TemperatureMax = maxTemp;
            }
        }

        if (data.TryGetProperty("growth", out var g2) && g2.TryGetProperty("description", out var desc))
        {
            var text = desc.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                partial.CareSummary = text.Length > 500 ? text[..500] + "…" : text;
            }
        }

        return partial;
    }

    private async Task<ExternalKnowledgePartial?> FetchWikipediaSummaryAsync(ExternalSpeciesResult species, string keyword, CancellationToken cancellationToken)
    {
        var titles = new List<string>();
        if (ContainsCjk(keyword))
        {
            titles.Add(keyword);
        }

        if (!string.IsNullOrWhiteSpace(species.ScientificName))
        {
            titles.Add(species.ScientificName);
        }

        if (!string.IsNullOrWhiteSpace(species.Genus))
        {
            titles.Add(species.Genus);
        }

        foreach (var title in titles.Distinct())
        {
            foreach (var lang in new[] { "zh", "en" })
            {
                var summary = await FetchWikipediaSummaryByTitleAsync(lang, title, cancellationToken);
                if (summary != null)
                {
                    return summary;
                }
            }
        }

        return null;
    }

    private async Task<ExternalKnowledgePartial?> FetchWikipediaSummaryByTitleAsync(string lang, string title, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ExternalPlantApi");
            var url = $"https://{lang}.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(title)}";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var extract = document.RootElement.TryGetProperty("extract", out var extractEl) ? extractEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(extract))
            {
                return null;
            }

            return new ExternalKnowledgePartial
            {
                CareSummary = extract.Length > 600 ? extract[..600] + "…" : extract,
                Provider = $"Wikipedia-{lang}"
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool ContainsCjk(string text) => text.Any(c => c >= 0x4E00 && c <= 0x9FFF);

    private static string DescribeLightScale(int value) => value switch
    {
        >= 8 => "偏好強光或全日照（Trefle 光照指數偏高）",
        >= 5 => "明亮散射光、半日照",
        _ => "可耐半陰或散射光"
    };

    private static string DescribeWaterScale(int value) => value switch
    {
        >= 7 => "偏好濕潤介質，需較頻繁澆水",
        >= 4 => "介質略乾再澆，中等澆水頻率",
        _ => "耐旱，土乾再澆、避免積水"
    };

    private static string DescribeHumidityScale(int value) => value switch
    {
        >= 7 => "偏好較高空气湿度（約 60% 以上）",
        >= 4 => "中等湿度（約 40–60%）",
        _ => "偏低湿度即可，注意通风"
    };

    private async Task<ExternalPlantSearchResult?> SearchTrefleAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Trefle.ApiKey))
        {
            _logger.LogWarning("Trefle API key is not configured.");
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("ExternalPlantApi");
            var url = $"{_options.Trefle.BaseUrl.TrimEnd('/')}/plants/search?q={Uri.EscapeDataString(keyword)}&token={_options.Trefle.ApiKey}";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Trefle search failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
            {
                return null;
            }

            var first = data[0];
            var species = new ExternalSpeciesResult
            {
                ScientificName = first.GetProperty("scientific_name").GetString() ?? keyword,
                CommonName = first.TryGetProperty("common_name", out var cn) ? cn.GetString() : null,
                Genus = first.TryGetProperty("genus", out var genus) ? genus.GetString() : null,
                Family = first.TryGetProperty("family", out var family) ? family.GetString() : null,
                ImageUrl = first.TryGetProperty("image_url", out var img) ? img.GetString() : null,
                TaxonId = first.TryGetProperty("id", out var id) ? id.GetRawText() : null,
                SourceType = "Trefle",
                SourceId = first.TryGetProperty("id", out var sid) ? sid.GetRawText() : string.Empty,
                Provider = "Trefle"
            };

            return new ExternalPlantSearchResult
            {
                Species = species,
                Knowledge = new ExternalKnowledgeResult
                {
                    CareSummary = $"資料來源：Trefle。學名 {species.ScientificName}。",
                    Provider = "Trefle"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trefle search error for keyword {Keyword}", keyword);
            return null;
        }
    }

    private async Task<ExternalPlantSearchResult?> SearchINaturalistAsync(string keyword, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ExternalPlantApi");
            var url = $"{_options.INaturalist.BaseUrl.TrimEnd('/')}/taxa?q={Uri.EscapeDataString(keyword)}&iconic_taxa=Plantae&per_page=20";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("iNaturalist search failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                return null;
            }

            JsonElement? best = null;
            var bestScore = int.MinValue;
            foreach (var candidate in results.EnumerateArray())
            {
                var score = ScoreINaturalistCandidate(keyword, candidate);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best == null || bestScore < 0)
            {
                return null;
            }

            var first = best.Value;
            var (genus, family) = ExtractINaturalistTaxonomy(first);
            var species = new ExternalSpeciesResult
            {
                ScientificName = first.GetProperty("name").GetString() ?? keyword,
                CommonName = first.TryGetProperty("preferred_common_name", out var cn) ? cn.GetString() : null,
                ChineseName = ContainsCjk(keyword) ? keyword : null,
                Genus = genus,
                Family = family,
                TaxonId = first.TryGetProperty("id", out var id) ? id.GetRawText() : null,
                SourceType = "iNaturalist",
                SourceId = first.TryGetProperty("id", out var sid) ? sid.GetRawText() : string.Empty,
                Provider = "iNaturalist"
            };

            return new ExternalPlantSearchResult
            {
                Species = species,
                Knowledge = new ExternalKnowledgeResult
                {
                    CareSummary = $"資料來源：iNaturalist。學名 {species.ScientificName}。",
                    Provider = "iNaturalist"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iNaturalist search error for keyword {Keyword}", keyword);
            return null;
        }
    }

    private static bool IsAcceptableTaxonMatch(string keyword, ExternalSpeciesResult species)
    {
        if (ContainsCjk(keyword))
        {
            if (GenusCareTemplateProvider.TryGetFromKeyword(keyword) != null)
            {
                return true;
            }
        }

        var normalizedKeyword = keyword.Trim().ToLowerInvariant();
        var normalizedName = (species.ScientificName ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedName.Contains(normalizedKeyword, StringComparison.Ordinal) ||
            normalizedKeyword.Contains(normalizedName, StringComparison.Ordinal))
        {
            return true;
        }

        var keywordParts = normalizedKeyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return keywordParts.Length > 0 && keywordParts.All(part => normalizedName.Contains(part, StringComparison.Ordinal));
    }

    private static int ScoreINaturalistCandidate(string keyword, JsonElement taxon)
    {
        if (!IsPlantTaxon(taxon))
        {
            return -1;
        }

        var rank = taxon.TryGetProperty("rank", out var rankEl) ? rankEl.GetString() : null;
        var name = taxon.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
        var normalizedKeyword = keyword.Trim().ToLowerInvariant();
        var normalizedName = name.Trim().ToLowerInvariant();

        var score = rank switch
        {
            "species" => 100,
            "subspecies" or "variety" or "form" => 90,
            "genus" => 70,
            "hybrid" => 60,
            _ => 10
        };

        if (normalizedName == normalizedKeyword)
        {
            score += 200;
        }
        else if (normalizedName.StartsWith(normalizedKeyword, StringComparison.Ordinal) ||
                 normalizedKeyword.StartsWith(normalizedName, StringComparison.Ordinal))
        {
            score += 120;
        }
        else
        {
            var keywordParts = normalizedKeyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (keywordParts.Length > 0 && keywordParts.All(part => normalizedName.Contains(part, StringComparison.Ordinal)))
            {
                score += 80;
            }
            else if (ContainsCjk(keyword))
            {
                score -= 50;
            }
        }

        return score;
    }

    private static bool IsPlantTaxon(JsonElement taxon)
    {
        if (taxon.TryGetProperty("ancestors", out var ancestors))
        {
            foreach (var ancestor in ancestors.EnumerateArray())
            {
                if (ancestor.TryGetProperty("rank", out var rankEl) &&
                    ancestor.TryGetProperty("name", out var nameEl) &&
                    rankEl.GetString() == "kingdom")
                {
                    return string.Equals(nameEl.GetString(), "Plantae", StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        return true;
    }

    private async Task<ExternalPlantSearchResult?> SearchGbifAsync(string keyword, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ExternalPlantApi");
            var url = $"{_options.GBIF.BaseUrl.TrimEnd('/')}/species/match?name={Uri.EscapeDataString(keyword)}";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GBIF search failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("scientificName", out var scientificName))
            {
                return null;
            }

            var species = new ExternalSpeciesResult
            {
                ScientificName = scientificName.GetString() ?? keyword,
                CommonName = document.RootElement.TryGetProperty("canonicalName", out var cn) ? cn.GetString() : null,
                Genus = document.RootElement.TryGetProperty("genus", out var genus) ? genus.GetString() : null,
                Family = document.RootElement.TryGetProperty("family", out var family) ? family.GetString() : null,
                TaxonId = document.RootElement.TryGetProperty("usageKey", out var id) ? id.GetRawText() : null,
                SourceType = "GBIF",
                SourceId = document.RootElement.TryGetProperty("usageKey", out var sid) ? sid.GetRawText() : string.Empty,
                Provider = "GBIF"
            };

            return new ExternalPlantSearchResult
            {
                Species = species,
                Knowledge = new ExternalKnowledgeResult
                {
                    CareSummary = $"資料來源：GBIF。學名 {species.ScientificName}。",
                    Provider = "GBIF"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GBIF search error for keyword {Keyword}", keyword);
            return null;
        }
    }

    private static (string? Genus, string? Family) ExtractINaturalistTaxonomy(JsonElement taxon)
    {
        string? genus = null;
        string? family = null;

        if (taxon.TryGetProperty("ancestors", out var ancestors))
        {
            foreach (var ancestor in ancestors.EnumerateArray())
            {
                if (!ancestor.TryGetProperty("rank", out var rankEl) || !ancestor.TryGetProperty("name", out var nameEl))
                {
                    continue;
                }

                var rank = rankEl.GetString();
                var name = nameEl.GetString();
                if (rank == "genus")
                {
                    genus = name;
                }
                else if (rank == "family")
                {
                    family = name;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(genus) && taxon.TryGetProperty("name", out var nameProp))
        {
            var parts = nameProp.GetString()?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts?.Length >= 2)
            {
                genus = parts[0];
            }
        }

        return (genus, family);
    }
}

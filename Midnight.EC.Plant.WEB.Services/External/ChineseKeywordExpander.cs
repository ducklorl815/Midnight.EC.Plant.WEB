namespace Midnight.EC.Plant.WEB.Services.External;

/// <summary>
/// 常見中文品名 → 拉丁學名/搜尋詞，供 Trefle/GBIF/iNaturalist 查詢。
/// </summary>
public static class ChineseKeywordExpander
{
    private static readonly (string[] Triggers, string[] SearchTerms)[] Mappings =
    [
        (["玉露"], ["Haworthia cooperi", "Haworthia"]),
        (["十二卷"], ["Haworthiopsis fasciata", "Haworthiopsis"]),
        (["熊童子"], ["Cotyledon tomentosa"]),
        (["黑王子", "黑法师"], ["Aeonium arboreum"]),
        (["生石花"], ["Lithops"]),
        (["芦荟", "蘆薈"], ["Aloe vera"]),
        (["仙人掌"], ["Cactaceae"]),
        (["绿萝", "綠蘿"], ["Epipremnum aureum"]),
        (["吊兰", "吊蘭"], ["Chlorophytum comosum"]),
        (["虎皮兰", "虎尾蘭"], ["Sansevieria trifasciata", "Dracaena trifasciata"]),
        (["多肉"], ["Succulent"]),
        (["石莲", "石蓮"], ["Echeveria"]),
        (["景天"], ["Sedum"]),
        (["胧月"], ["Graptopetalum paraguayense"]),
        (["桃蛋"], ["Graptopetalum amethystinum"]),
        (["乙女心"], ["Sedum rubrotinctum"]),
        (["白掌"], ["Spathiphyllum"]),
        (["发财树", "發財樹"], ["Pachira aquatica"]),
        (["琴叶榕", "琴葉榕"], ["Ficus lyrata"]),
        (["龟背竹", "龜背竹"], ["Monstera deliciosa"]),
        (["蝴蝶兰", "蝴蝶蘭"], ["Phalaenopsis"]),
        (["君子兰", "君子蘭"], ["Clivia miniata"]),
    ];

    public static IEnumerable<string> Expand(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        var terms = new List<string> { keyword.Trim() };
        foreach (var (triggers, searchTerms) in Mappings)
        {
            if (triggers.Any(t => keyword.Contains(t, StringComparison.Ordinal)))
            {
                terms.AddRange(searchTerms);
            }
        }

        return terms.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public static string? TryResolvePrimaryLatinName(string keyword)
    {
        foreach (var term in Expand(keyword))
        {
            if (!ContainsCjk(term))
            {
                return term;
            }
        }

        return null;
    }

    private static bool ContainsCjk(string text) =>
        text.Any(c => c >= 0x4E00 && c <= 0x9FFF);
}

namespace Midnight.EC.Plant.WEB.Models.Enums;

public enum PlacementType
{
    Indoor = 1,
    Outdoor = 2,
    Balcony = 3
}

/// <summary>建議／實際日照四檔。</summary>
public enum LightLevel
{
    None = 1,
    Diffuse = 2,
    HalfDay = 3,
    FullSun = 4
}

public static class LightLevelDisplay
{
    public static string ToLabel(LightLevel? level) => level switch
    {
        LightLevel.None => "無日照／陰處",
        LightLevel.Diffuse => "散射",
        LightLevel.HalfDay => "半日",
        LightLevel.FullSun => "烈日",
        _ => "未設定"
    };

    public static LightLevel? TryParseFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var t = text.ToLowerInvariant();
        if (t.Contains("烈日") || t.Contains("全日照") || t.Contains("強光") || t.Contains("full sun"))
        {
            return LightLevel.FullSun;
        }

        if (t.Contains("半日") || t.Contains("半日照") || t.Contains("部分日照"))
        {
            return LightLevel.HalfDay;
        }

        if (t.Contains("散射") || t.Contains("明亮") || t.Contains("indirect") || t.Contains("bright"))
        {
            return LightLevel.Diffuse;
        }

        if (t.Contains("陰") || t.Contains("無日") || t.Contains("低光") || t.Contains("shade"))
        {
            return LightLevel.None;
        }

        return null;
    }
}

public static class PlacementTypeDisplay
{
    public static string ToLabel(PlacementType? placement) => placement switch
    {
        PlacementType.Indoor => "室內",
        PlacementType.Outdoor => "室外",
        PlacementType.Balcony => "陽台",
        _ => "未設定"
    };
}

public static class TaiwanCities
{
    public static readonly string[] All =
    [
        "臺北市", "新北市", "基隆市", "桃園市", "新竹市", "新竹縣",
        "苗栗縣", "臺中市", "彰化縣", "南投縣", "雲林縣", "嘉義市", "嘉義縣",
        "臺南市", "高雄市", "屏東縣", "宜蘭縣", "花蓮縣", "臺東縣",
        "澎湖縣", "金門縣", "連江縣"
    ];
}

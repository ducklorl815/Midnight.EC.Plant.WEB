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

/// <summary>盆底水盤實際使用情況（三態）。</summary>
public enum SaucerState
{
    /// <summary>無水盤</summary>
    None = 1,
    /// <summary>有水盤但不積水</summary>
    PresentDry = 2,
    /// <summary>有水盤且會蓄水</summary>
    PresentWithWater = 3
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

    /// <summary>選項下方固定說明（台灣居家／陽台情境）。</summary>
    public static string ToHelp(LightLevel level) => level switch
    {
        LightLevel.None => "幾乎無直射日：北向房間深處、走廊、或全天被建築遮住。適合耐陰植物。",
        LightLevel.Diffuse => "明亮但無長時間直射：窗邊紗簾後、東／西向短暫掠過、或樹蔭下的亮處。",
        LightLevel.HalfDay => "每天約 3～6 小時直射日：多數開放陽台、上午或下午有一段太陽。",
        LightLevel.FullSun => "每天約 6 小時以上強烈直射：南向無遮、頂樓或空曠露台；夏季易曬傷忌烈日物種。",
        _ => string.Empty
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

        if (t.Contains("半日") || t.Contains("半日照") || t.Contains("部分日照") || t.Contains("喜光"))
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

public static class SaucerStateDisplay
{
    public static string ToLabel(SaucerState? state) => state switch
    {
        SaucerState.None => "無水盤",
        SaucerState.PresentDry => "有水盤但不積水",
        SaucerState.PresentWithWater => "有水盤且會蓄水",
        _ => "未設定"
    };

    public const string PurposeHint =
        "水盤主要用來接住多餘的水、避免弄髒地面，不是把盆土「蓄水期拉長」。多數植物忌水盤長期積水（易悶根）；少數喜濕植物才適合淺盤保濕。";
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

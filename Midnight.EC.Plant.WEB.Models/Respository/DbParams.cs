using System.Reflection;
using Dapper;

namespace Midnight.EC.Plant.WEB.Models.Respository;

/// <summary>
/// 組 Dapper 參數時略過相容別名（Id/PlantId…），避免與 ID/PlantID 在 SQL Server 參數名撞名。
/// </summary>
public static class DbParams
{
    public static DynamicParameters From(object model)
    {
        var type = model.GetType();
        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToList();
        var names = props.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        var dp = new DynamicParameters();
        foreach (var prop in props)
        {
            if (IsAlias(prop.Name, names)) continue;
            if (IsNavigation(prop)) continue;

            dp.Add(prop.Name, prop.GetValue(model));
        }

        return dp;
    }

    private static bool IsAlias(string name, HashSet<string> names) =>
        name switch
        {
            "Id" when names.Contains("ID") => true,
            "PlantId" when names.Contains("PlantID") => true,
            "SpeciesId" when names.Contains("SpeciesID") => true,
            "DiaryId" when names.Contains("DiaryID") => true,
            "ImageId" when names.Contains("ImageID") => true,
            "OriginalPhotoId" when names.Contains("OriginalPhotoID") => true,
            "AnalysisId" when names.Contains("AnalysisID") => true,
            "SourceId" when names.Contains("SourceID") => true,
            "CreatedAt" or "UpdatedAt" or "IsActive" => true,
            _ => false
        };

    private static bool IsNavigation(PropertyInfo prop)
    {
        if (prop.PropertyType == typeof(string) || prop.PropertyType.IsValueType) return false;
        if (Nullable.GetUnderlyingType(prop.PropertyType)?.IsValueType == true) return false;
        var ns = prop.PropertyType.Namespace ?? "";
        return ns.Contains("Midnight.EC.Plant.WEB.Models", StringComparison.Ordinal);
    }
}

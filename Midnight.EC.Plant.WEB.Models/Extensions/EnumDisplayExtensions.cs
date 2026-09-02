using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Midnight.EC.Plant.WEB.Models.Extensions;

public static class EnumDisplayExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var display = member?.GetCustomAttribute<DisplayAttribute>();
        return display?.Name ?? value.ToString();
    }
}

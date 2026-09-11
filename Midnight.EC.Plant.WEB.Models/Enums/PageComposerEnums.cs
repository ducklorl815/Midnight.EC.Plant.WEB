using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Midnight.EC.Plant.WEB.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PageModuleType
{
    [Display(Name = "Banner")]
    Banner = 0,

    [Display(Name = "左圖右文")]
    LeftImageRightText = 1,

    [Display(Name = "右圖左文")]
    RightImageLeftText = 2,

    [Display(Name = "多圖多文")]
    MultiImageMultiText = 3,

    [Display(Name = "多圖風格")]
    MultiImageStyle = 4,

    [Display(Name = "左圖右文·植栽細節牆·輪播")]
    PlantDetailWallCarousel = 5,

    [Display(Name = "通知提醒")]
    NotificationReminders = 6
}

using System.ComponentModel.DataAnnotations;

namespace Midnight.EC.Plant.WEB.Models.Enums;

public enum CareRecordType
{
    [Display(Name = "澆水")]
    Watering = 0,

    [Display(Name = "施肥")]
    Fertilizing = 1,

    [Display(Name = "光照")]
    Light = 2,

    [Display(Name = "溫度")]
    Temperature = 3,

    [Display(Name = "濕度")]
    Humidity = 4
}

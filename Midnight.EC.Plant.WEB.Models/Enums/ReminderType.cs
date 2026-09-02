using System.ComponentModel.DataAnnotations;

namespace Midnight.EC.Plant.WEB.Models.Enums;

public enum ReminderType
{
    [Display(Name = "澆水")]
    Watering = 0,

    [Display(Name = "施肥")]
    Fertilizing = 1,

    [Display(Name = "分析")]
    Analysis = 2,

    [Display(Name = "環境")]
    Environment = 3,

    [Display(Name = "AI 警示")]
    AiAlert = 4,

    [Display(Name = "自訂")]
    Custom = 5
}

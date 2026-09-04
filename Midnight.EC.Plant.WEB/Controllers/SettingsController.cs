using Microsoft.AspNetCore.Mvc;

namespace Midnight.EC.Plant.WEB.Controllers;

public class SettingsController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}

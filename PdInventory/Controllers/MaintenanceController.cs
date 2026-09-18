using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>「網站維護中」畫面。導向規則見 <see cref="MaintenanceModeExtensions.UseMaintenanceMode"/>。</summary>
[AllowAnonymous]
public class MaintenanceController : Controller
{
    private readonly MaintenanceOptions _options;

    public MaintenanceController(MaintenanceOptions options) => _options = options;

    [Route(MaintenanceModeExtensions.Path)]
    public IActionResult Index()
    {
        // 維護已結束還停在這個網址（書籤、瀏覽器重新整理）就送回首頁
        if (!_options.Enabled) return RedirectToAction("Index", "Home");

        // 不要讓瀏覽器或中間的代理快取這個畫面，否則維護結束後還會看到它
        Response.Headers.CacheControl = "no-store, no-cache";
        return View(_options);
    }
}

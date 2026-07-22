using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.InventoryCount = await _db.InventoryItems.CountAsync();
        ViewBag.TransferCount = await _db.TransferRecords.CountAsync();
        ViewBag.SystemCount = await _db.InfoSystems.CountAsync();
        ViewBag.CategoryCount = await _db.Categories.CountAsync();
        ViewBag.PurposeCount = await _db.Purposes.CountAsync();
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

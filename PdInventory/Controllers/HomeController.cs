using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
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
        // 盤點與資產清單（SW/DA 與資訊系統盤點表同為 InfoSystems 的不同檢視面向；
        // 風險自評與個資檔案盤點表同為 InventoryItems）
        ViewBag.SystemCount = await _db.InfoSystems.CountAsync();
        ViewBag.InventoryCount = await _db.InventoryItems.CountAsync();
        ViewBag.TransferCount = await _db.TransferRecords.CountAsync();

        // 維護資料
        ViewBag.CategoryCount = await _db.Categories.CountAsync();
        ViewBag.PurposeCount = await _db.Purposes.CountAsync();
        ViewBag.RiskCategoryCount = await _db.RiskCategories.CountAsync();
        ViewBag.RiskImpactCount = await _db.RiskImpactLevels.CountAsync();
        ViewBag.RiskLikelihoodCount = await _db.RiskLikelihoodLevels.CountAsync();
        ViewBag.RiskEffectivenessCount = await _db.RiskEffectivenessLevels.CountAsync();
        return View();
    }

    /// <summary>錯誤頁不要求登入：登入流程本身出錯時也得顯示得出來。</summary>
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

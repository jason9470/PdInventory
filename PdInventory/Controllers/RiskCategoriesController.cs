using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>3-1：風險分類編號（維護檔）</summary>
// 主管看得到但不能改（0918）：類別層級只要求可檢視，修改動作另外掛 Admin
[Authorize(Policy = Policies.ViewMaintenance)]
public class RiskCategoriesController : Controller
{
    private readonly AppDbContext _db;
    public RiskCategoriesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.RiskCategories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(r => r.Code.Contains(q)
                                  || r.CategoryName.Contains(q)
                                  || r.EventDescription.Contains(q));

        ViewBag.Query = q;
        ViewBag.UsageCounts = await LookupUsage.CountsAsync(_db, UsageKind.RiskCategory);
        return View(await query.OrderBy(r => r.Code).ToListAsync());
    }

    [Authorize(Policy = Policies.Admin)]
    public IActionResult Create() => View("Form", new RiskCategory());

    [Authorize(Policy = Policies.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RiskCategory model)
    {
        await ValidateUniqueCodeAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.RiskCategories.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增風險分類「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.RiskCategories.FindAsync(id);
        if (model is null) return NotFound();
        return View("Form", model);
    }

    [Authorize(Policy = Policies.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RiskCategory model)
    {
        if (id != model.Id) return BadRequest();
        await ValidateUniqueCodeAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.Update(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新風險分類「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Policies.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.RiskCategories.FindAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));

        // 風險自評存的是文字而不是外鍵，資料庫不會擋——刪掉還在用的分類，
        // 那些資料就變成查不到來源的孤兒值，畫面上還不會有任何警告
        var used = await _db.InventoryItems.CountAsync(i => i.RiskCategoryCode == model.Code);
        if (used > 0)
        {
            TempData["Error"] = $"「{model.Label}」還被 {used} 筆風險自評使用中，請先改掉那些資料再刪除。";
            return RedirectToAction(nameof(Index));
        }

        _db.RiskCategories.Remove(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已刪除風險分類「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueCodeAsync(RiskCategory model)
    {
        var duplicated = await _db.RiskCategories
            .AnyAsync(r => r.Code == model.Code && r.Id != model.Id);
        if (duplicated)
            ModelState.AddModelError(nameof(RiskCategory.Code), $"風險分類編號 {model.Code} 已存在");
    }
}

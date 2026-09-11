using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>3-3：評估發生可能性（維護檔）</summary>
[Authorize(Policy = Policies.Admin)]
public class RiskLikelihoodsController : Controller
{
    private readonly AppDbContext _db;
    public RiskLikelihoodsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var levels = await _db.RiskLikelihoodLevels.OrderBy(r => r.Level).ToListAsync();
        ViewBag.UsageCounts = await LookupUsage.CountsAsync(_db, UsageKind.RiskLikelihood);
        return View(levels);
    }

    public IActionResult Create() => View("Form", new RiskLikelihoodLevel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RiskLikelihoodLevel model)
    {
        await ValidateUniqueLevelAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.RiskLikelihoodLevels.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增發生可能性「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.RiskLikelihoodLevels.FindAsync(id);
        if (model is null) return NotFound();
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RiskLikelihoodLevel model)
    {
        if (id != model.Id) return BadRequest();
        await ValidateUniqueLevelAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.Update(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新發生可能性「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.RiskLikelihoodLevels.FindAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));

        // 風險自評存的是文字而不是外鍵，資料庫不會擋。既有資料有「2：中等」與只存
        // 「2」兩種寫法，兩種都要算成使用中，否則會誤判成沒人用而放行刪除。
        var items = await _db.InventoryItems.Select(i => i.RiskLikelihoodLevel).ToListAsync();
        var used = items.Count(v => v == model.Label || v == model.Level.ToString());
        if (used > 0)
        {
            TempData["Error"] = $"「{model.Label}」還被 {used} 筆風險自評使用中，請先改掉那些資料再刪除。";
            return RedirectToAction(nameof(Index));
        }

        _db.RiskLikelihoodLevels.Remove(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已刪除發生可能性「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueLevelAsync(RiskLikelihoodLevel model)
    {
        var duplicated = await _db.RiskLikelihoodLevels
            .AnyAsync(r => r.Level == model.Level && r.Id != model.Id);
        if (duplicated)
            ModelState.AddModelError(nameof(RiskLikelihoodLevel.Level), $"等級 {model.Level} 已存在");
    }
}

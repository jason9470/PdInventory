using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>3-4：有效性評估（維護檔）</summary>
[Authorize(Policy = Policies.Admin)]
public class RiskEffectivenessController : Controller
{
    private readonly AppDbContext _db;
    public RiskEffectivenessController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var levels = await _db.RiskEffectivenessLevels.OrderBy(r => r.Level).ToListAsync();
        ViewBag.UsageCounts = await LookupUsage.RiskLevelsAsync(
            _db, i => i.RiskEffectivenessLevel, levels, l => l.Level, l => l.Label);
        return View(levels);
    }

    public IActionResult Create() => View("Form", new RiskEffectivenessLevel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RiskEffectivenessLevel model)
    {
        await ValidateUniqueLevelAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.RiskEffectivenessLevels.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增有效性等級「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.RiskEffectivenessLevels.FindAsync(id);
        if (model is null) return NotFound();
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RiskEffectivenessLevel model)
    {
        if (id != model.Id) return BadRequest();
        await ValidateUniqueLevelAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.Update(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新有效性等級「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.RiskEffectivenessLevels.FindAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));

        // 風險自評存的是文字而不是外鍵，資料庫不會擋。這一欄的既有資料只存「2」，
        // 畫面上選的卻是「2：普通」，兩種寫法都要算成使用中。
        var items = await _db.InventoryItems.Select(i => i.RiskEffectivenessLevel).ToListAsync();
        var used = items.Count(v => v == model.Label || v == model.Level.ToString());
        if (used > 0)
        {
            TempData["Error"] = $"「{model.Label}」還被 {used} 筆風險自評使用中，請先改掉那些資料再刪除。";
            return RedirectToAction(nameof(Index));
        }

        _db.RiskEffectivenessLevels.Remove(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已刪除有效性等級「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueLevelAsync(RiskEffectivenessLevel model)
    {
        var duplicated = await _db.RiskEffectivenessLevels
            .AnyAsync(r => r.Level == model.Level && r.Id != model.Id);
        if (duplicated)
            ModelState.AddModelError(nameof(RiskEffectivenessLevel.Level), $"等級 {model.Level} 已存在");
    }
}

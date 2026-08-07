using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Controllers;

/// <summary>3-4：有效性評估（維護檔）</summary>
public class RiskEffectivenessController : Controller
{
    private readonly AppDbContext _db;
    public RiskEffectivenessController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.RiskEffectivenessLevels.OrderBy(r => r.Level).ToListAsync());

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
        if (model is not null)
        {
            _db.RiskEffectivenessLevels.Remove(model);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除有效性等級「{model.Label}」";
        }
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

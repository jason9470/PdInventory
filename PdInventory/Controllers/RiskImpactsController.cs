using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>3-2：評估影響程度（維護檔）</summary>
[Authorize(Policy = Policies.Admin)]
public class RiskImpactsController : Controller
{
    private readonly AppDbContext _db;
    public RiskImpactsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.RiskImpactLevels.OrderBy(r => r.Level).ToListAsync());

    public IActionResult Create() => View("Form", new RiskImpactLevel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RiskImpactLevel model)
    {
        await ValidateUniqueLevelAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.RiskImpactLevels.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增影響程度「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.RiskImpactLevels.FindAsync(id);
        if (model is null) return NotFound();
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RiskImpactLevel model)
    {
        if (id != model.Id) return BadRequest();
        await ValidateUniqueLevelAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.Update(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新影響程度「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.RiskImpactLevels.FindAsync(id);
        if (model is not null)
        {
            _db.RiskImpactLevels.Remove(model);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除影響程度「{model.Label}」";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueLevelAsync(RiskImpactLevel model)
    {
        var duplicated = await _db.RiskImpactLevels
            .AnyAsync(r => r.Level == model.Level && r.Id != model.Id);
        if (duplicated)
            ModelState.AddModelError(nameof(RiskImpactLevel.Level), $"等級 {model.Level} 已存在");
    }
}

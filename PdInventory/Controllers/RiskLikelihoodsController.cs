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
        => View(await _db.RiskLikelihoodLevels.OrderBy(r => r.Level).ToListAsync());

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
        if (model is not null)
        {
            _db.RiskLikelihoodLevels.Remove(model);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除發生可能性「{model.Label}」";
        }
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

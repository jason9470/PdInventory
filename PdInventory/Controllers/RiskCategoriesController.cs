using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Controllers;

/// <summary>3-1：風險分類編號（維護檔）</summary>
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
        return View(await query.OrderBy(r => r.Code).ToListAsync());
    }

    public IActionResult Create() => View("Form", new RiskCategory());

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

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.RiskCategories.FindAsync(id);
        if (model is null) return NotFound();
        return View("Form", model);
    }

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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.RiskCategories.FindAsync(id);
        if (model is not null)
        {
            _db.RiskCategories.Remove(model);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除風險分類「{model.Label}」";
        }
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

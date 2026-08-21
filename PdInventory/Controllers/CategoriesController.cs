using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>附表一：法務部公告個人資料類別（維護檔）</summary>
[Authorize(Policy = Policies.Admin)]
public class CategoriesController : Controller
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Categories.Include(c => c.InventoryItems).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(c => c.Code.Contains(q)
                                  || c.DisplayCode.Contains(q)
                                  || c.Name.Contains(q)
                                  || c.GroupName.Contains(q)
                                  || (c.Example != null && c.Example.Contains(q)));

        ViewBag.Query = q;
        return View(await query.OrderBy(c => c.Code).ToListAsync());
    }

    public IActionResult Create() => View("Form", new PdCategory());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PdCategory category)
    {
        await ValidateUniqueCodeAsync(category);
        if (!ModelState.IsValid) return View("Form", category);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增個資類別「{category.Label}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();
        return View("Form", category);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PdCategory category)
    {
        if (id != category.Id) return BadRequest();
        await ValidateUniqueCodeAsync(category);
        if (!ModelState.IsValid) return View("Form", category);
        _db.Update(category);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新個資類別「{category.Label}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories
            .Include(c => c.InventoryItems)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (category is not null)
        {
            if (category.InventoryItems.Count > 0)
            {
                TempData["Error"] = $"個資類別「{category.Label}」仍被 {category.InventoryItems.Count} 筆盤點項目使用，無法刪除。";
                return RedirectToAction(nameof(Index));
            }
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除個資類別「{category.Label}」";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueCodeAsync(PdCategory category)
    {
        var duplicated = await _db.Categories
            .AnyAsync(c => c.Code == category.Code && c.Id != category.Id);
        if (duplicated)
            ModelState.AddModelError(nameof(PdCategory.Code), $"代號 {category.Code} 已存在");
    }
}

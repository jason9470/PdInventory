using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>附表二：法務部公告特定目的列表（維護檔）</summary>
// 主管看得到但不能改（0918）：類別層級只要求可檢視，修改動作另外掛 Admin
[Authorize(Policy = Policies.ViewMaintenance)]
public class PurposesController : Controller
{
    private readonly AppDbContext _db;
    public PurposesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Purposes.Include(p => p.InventoryItems).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Code.Contains(q) || p.Name.Contains(q));

        ViewBag.Query = q;
        ViewBag.UsageCounts = await LookupUsage.CountsAsync(_db, UsageKind.Purpose);
        return View(await query.OrderBy(p => p.Code).ToListAsync());
    }

    [Authorize(Policy = Policies.Admin)]
    public IActionResult Create() => View("Form", new Purpose());

    [Authorize(Policy = Policies.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Purpose purpose)
    {
        await ValidateUniqueCodeAsync(purpose);
        if (!ModelState.IsValid) return View("Form", purpose);
        _db.Purposes.Add(purpose);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增特定目的「{purpose.Label}」";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var purpose = await _db.Purposes.FindAsync(id);
        if (purpose is null) return NotFound();
        return View("Form", purpose);
    }

    [Authorize(Policy = Policies.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Purpose purpose)
    {
        if (id != purpose.Id) return BadRequest();
        await ValidateUniqueCodeAsync(purpose);
        if (!ModelState.IsValid) return View("Form", purpose);
        _db.Update(purpose);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新特定目的「{purpose.Label}」";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Policies.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var purpose = await _db.Purposes
            .Include(p => p.InventoryItems)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (purpose is not null)
        {
            if (purpose.InventoryItems.Count > 0)
            {
                TempData["Error"] = $"特定目的「{purpose.Label}」仍被 {purpose.InventoryItems.Count} 筆盤點項目使用，無法刪除。";
                return RedirectToAction(nameof(Index));
            }
            _db.Purposes.Remove(purpose);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除特定目的「{purpose.Label}」";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueCodeAsync(Purpose purpose)
    {
        var duplicated = await _db.Purposes
            .AnyAsync(p => p.Code == purpose.Code && p.Id != purpose.Id);
        if (duplicated)
            ModelState.AddModelError(nameof(Purpose.Code), $"代號 {purpose.Code} 已存在");
    }
}

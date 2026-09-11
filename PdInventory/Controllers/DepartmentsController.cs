using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>部門（共用維護檔）。六張表都會用到，因此掛在側邊欄的「共用」底下。</summary>
[Authorize(Policy = Policies.Admin)]
public class DepartmentsController : Controller
{
    private readonly AppDbContext _db;
    public DepartmentsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Departments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(d => d.CostCenter.Contains(q)
                                  || d.Name.Contains(q)
                                  || d.Remark.Contains(q));

        ViewBag.Query = q;
        ViewBag.UsageCounts = await LookupUsage.CountsAsync(_db, UsageKind.Department);
        // 代號留空的排在最後：它們是資料裡有、甲方清單沒有的，待補
        return View(await query.OrderBy(d => d.CostCenter == "")
                               .ThenBy(d => d.CostCenter)
                               .ThenBy(d => d.Name)
                               .ToListAsync());
    }

    public IActionResult Create() => View("Form", new Department());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department model)
    {
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.Departments.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增部門「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.Departments.FindAsync(id);
        if (model is null) return NotFound();
        ViewBag.UsedBy = await CountUsageAsync(model.Name);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Department model)
    {
        if (id != model.Id) return BadRequest();
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid)
        {
            ViewBag.UsedBy = await CountUsageAsync(model.Name);
            return View("Form", model);
        }
        _db.Update(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新部門「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.Departments.FindAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));

        // 還被資料引用就不給刪：刪掉之後那些欄位的值會變成「不在選項中」的孤兒
        var used = await CountUsageAsync(model.Name);
        if (used > 0)
        {
            TempData["Error"] = $"「{model.Name}」還被 {used} 筆資料使用中，請先改掉那些資料再刪除。";
            return RedirectToAction(nameof(Index));
        }

        _db.Departments.Remove(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已刪除部門「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 有多少筆資料用到這個部門。與清單上的「使用中」和明細視窗走同一支，
    /// 三處各寫一套遲早會對不上。
    /// </summary>
    private Task<int> CountUsageAsync(string name) =>
        LookupUsage.CountAsync(_db, UsageKind.Department, name);

    private async Task ValidateUniqueNameAsync(Department model)
    {
        if (await _db.Departments.AnyAsync(d => d.Name == model.Name && d.Id != model.Id))
            ModelState.AddModelError(nameof(Department.Name), $"部門「{model.Name}」已存在");
    }
}

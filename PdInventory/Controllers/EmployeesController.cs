using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>人員（共用維護檔）。資訊系統開發一部的組織名冊。</summary>
[Authorize(Policy = Policies.Admin)]
public class EmployeesController : Controller
{
    private readonly AppDbContext _db;
    public EmployeesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Employees.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(e => e.EmpNo.Contains(q)
                                  || e.Name.Contains(q)
                                  || e.DepartmentName.Contains(q)
                                  || e.TeamName.Contains(q)
                                  || e.Section.Contains(q)
                                  || e.Remark.Contains(q));

        ViewBag.Query = q;
        // 組別待補的排在最後：那些是資料裡有、名冊沒有的，等甲方補
        return View(await query.OrderBy(e => e.TeamName == "")
                               .ThenBy(e => e.TeamName)
                               .ThenBy(e => e.Section != Employee.ManagerSection)
                               .ThenBy(e => e.Name)
                               .ToListAsync());
    }

    public IActionResult Create() => View("Form", new Employee());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee model)
    {
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.Employees.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增人員「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.Employees.FindAsync(id);
        if (model is null) return NotFound();
        ViewBag.UsedBy = await CountUsageAsync(model.Name);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Employee model)
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
        TempData["Message"] = $"已更新人員「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.Employees.FindAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));

        var used = await CountUsageAsync(model.Name);
        if (used > 0)
        {
            TempData["Error"] = $"「{model.Name}」還被 {used} 筆資料使用中，請先改掉那些資料再刪除。";
            return RedirectToAction(nameof(Index));
        }

        _db.Employees.Remove(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已刪除人員「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 有多少筆資料用到這個人。五個對照欄位裡有兩個是多選（以 / 分隔），
    /// 不能用等號比對，理由同 DepartmentsController。同一筆多個欄位都是他只算一筆。
    /// </summary>
    private async Task<int> CountUsageAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;

        var systems = await _db.InfoSystems
            .Select(s => new { s.SwAppManager, s.SwAppMaintainer, s.SwDeveloper, s.SwReviewer })
            .ToListAsync();
        var dataAssets = await _db.DataAssets.Select(d => d.DaReviewer).ToListAsync();

        return systems.Count(s => MultiValue.Contains(s.SwAppManager, name)
                               || MultiValue.Contains(s.SwAppMaintainer, name)
                               || MultiValue.Contains(s.SwDeveloper, name)
                               || MultiValue.Contains(s.SwReviewer, name))
             + dataAssets.Count(d => MultiValue.Contains(d, name));
    }

    private async Task ValidateUniqueNameAsync(Employee model)
    {
        if (await _db.Employees.AnyAsync(e => e.Name == model.Name && e.Id != model.Id))
            ModelState.AddModelError(nameof(Employee.Name), $"人員「{model.Name}」已存在");
    }
}

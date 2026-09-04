using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>資管維運人員（共用維護檔）。對照「SW-維運人員」。</summary>
[Authorize(Policy = Policies.Admin)]
public class OpsStaffController : Controller
{
    private readonly AppDbContext _db;
    public OpsStaffController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.OpsStaffs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(o => o.TeamName.Contains(q)
                                  || o.Name.Contains(q)
                                  || o.Remark.Contains(q));

        ViewBag.Query = q;
        ViewBag.UsageCounts = await LookupUsage.OpsStaffAsync(_db);
        // 組別留空的排在最後：它們是資料裡有、甲方清單沒有的，待補
        return View(await query.OrderBy(o => o.TeamName == "")
                               .ThenBy(o => o.TeamName)
                               .ThenBy(o => o.Name)
                               .ToListAsync());
    }

    public IActionResult Create() => View("Form", new OpsStaff());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpsStaff model)
    {
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.OpsStaffs.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增維運人員「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.OpsStaffs.FindAsync(id);
        if (model is null) return NotFound();
        ViewBag.UsedBy = await CountUsageAsync(model.Name);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpsStaff model)
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
        TempData["Message"] = $"已更新維運人員「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.OpsStaffs.FindAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));

        var used = await CountUsageAsync(model.Name);
        if (used > 0)
        {
            TempData["Error"] = $"「{model.Name}」還被 {used} 筆資料使用中，請先改掉那些資料再刪除。";
            return RedirectToAction(nameof(Index));
        }

        _db.OpsStaffs.Remove(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已刪除維運人員「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 有多少筆資產用到這個人。維運人員與程式換版人員都是多選欄位（以 / 分隔），
    /// 不能用等號比對，理由同 DepartmentsController。同一筆兩個欄位都掛他只算一筆。
    /// </summary>
    private async Task<int> CountUsageAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;

        var rows = await _db.InfoSystems
            .Select(s => new { s.SwOperator, s.SwDeployer })
            .ToListAsync();

        return rows.Count(r => MultiValue.Contains(r.SwOperator, name)
                            || MultiValue.Contains(r.SwDeployer, name));
    }

    private async Task ValidateUniqueNameAsync(OpsStaff model)
    {
        if (await _db.OpsStaffs.AnyAsync(o => o.Name == model.Name && o.Id != model.Id))
            ModelState.AddModelError(nameof(OpsStaff.Name), $"維運人員「{model.Name}」已存在");
    }
}

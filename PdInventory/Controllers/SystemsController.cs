using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Controllers;

/// <summary>Sheet3：資訊系統、資料庫與檔案伺服器盤點表</summary>
public class SystemsController : Controller
{
    private readonly AppDbContext _db;
    public SystemsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.InfoSystems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.SystemName.Contains(q)
                                  || s.Description.Contains(q)
                                  || s.DbName.Contains(q)
                                  || s.Remark.Contains(q));

        ViewBag.Query = q;
        return View(await query.OrderBy(s => s.SeqNo).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var system = await _db.InfoSystems.FindAsync(id);
        if (system is null) return NotFound();
        return View(system);
    }

    public IActionResult Create() => View("Form", new InfoSystem());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InfoSystem system)
    {
        if (!ModelState.IsValid) return View("Form", system);
        _db.InfoSystems.Add(system);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增系統「{system.SeqNo} {system.SystemName}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var system = await _db.InfoSystems.FindAsync(id);
        if (system is null) return NotFound();
        return View("Form", system);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InfoSystem system)
    {
        if (id != system.Id) return BadRequest();
        if (!ModelState.IsValid) return View("Form", system);

        var existing = await _db.InfoSystems.FindAsync(id);
        if (existing is null) return NotFound();

        ApplySystemFields(existing, system);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新系統「{system.SeqNo} {system.SystemName}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var system = await _db.InfoSystems.FindAsync(id);
        if (system is not null)
        {
            _db.InfoSystems.Remove(system);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除系統「{system.SeqNo} {system.SystemName}」";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>只複製系統基本欄位與 Sheet3 欄位，保留既有 SW 與 DA 資料。</summary>
    private static void ApplySystemFields(InfoSystem t, InfoSystem s)
    {
        t.SeqNo = s.SeqNo;
        t.SystemCode = s.SystemCode;
        t.SystemName = s.SystemName;
        t.Description = s.Description;
        t.DbName = s.DbName;

        t.BackupLocation = s.BackupLocation;
        t.BackupCycle = s.BackupCycle;

        t.ExternalUnitName = s.ExternalUnitName;
        t.HasLog = s.HasLog;
        t.AccessCreate = s.AccessCreate;
        t.AccessDelete = s.AccessDelete;
        t.AccessCopy = s.AccessCopy;
        t.FileDescription = s.FileDescription;
        t.SpecialData = s.SpecialData;
        t.SubjectCount = s.SubjectCount;
        t.RetentionPeriod = s.RetentionPeriod;

        t.Remark = s.Remark;
    }
}

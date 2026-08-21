using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>Sheet3：資訊系統、資料庫與檔案伺服器盤點表</summary>
public class SystemsController : Controller
{
    private readonly AppDbContext _db;
    public SystemsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.InfoSystems.AsQueryable();

        q = this.ResolveSearch(q);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.SystemCode.Contains(q)
                                  || s.SystemName.Contains(q));

        ViewBag.Query = q;
        return View(await query.OrderBy(s => s.SeqNo).ToListAsync());
    }

    /// <summary>匯出目前搜尋結果。q 由畫面帶入，與清單所見一致，不更動搜尋記憶。</summary>
    public async Task<IActionResult> Export(string? q)
    {
        var query = _db.InfoSystems.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.SystemCode.Contains(q)
                                  || s.SystemName.Contains(q));

        var rows = await query.OrderBy(s => s.SeqNo).ToListAsync();
        var (content, fileName) = await ExcelExporter.BuildAsync(_db, "Systems", rows);
        return File(content, ExcelExporter.ContentType, fileName);
    }

    /// <param name="from">來源清單頁，供[回列表]回到原處。</param>
    public async Task<IActionResult> Details(int id, string? from)
    {
        var system = await _db.InfoSystems.FindAsync(id);
        if (system is null) return NotFound();
        // 記住這筆的資產編號，之後進到任一清單頁都會自動帶入搜尋欄
        this.RememberSearch(system.SystemCode);
        ViewBag.From = ListSource.Resolve(from);
        return View(system);
    }

    /// <param name="from">從哪張清單按的[新增]，決定[取消]與新增後回到哪裡。</param>
    public IActionResult Create(string? from)
    {
        ViewBag.From = ListSource.Resolve(from);
        return View("CreateAll", new InfoSystem());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InfoSystem system, string? from)
    {
        var source = ListSource.Resolve(from);
        ViewBag.From = source;
        await ValidateUniqueKeysAsync(system);
        if (!ModelState.IsValid) return View("CreateAll", system);

        // 統一新增畫面一次送出 SW / DA / 系統盤點三個區塊，整筆一起建立
        _db.InfoSystems.Add(system);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增資訊資產「{system.SystemCode} {system.SystemName}」";
        return RedirectToAction("Index", source);
    }

    /// <param name="from">來源清單頁，供[取消]回到原處。</param>
    public async Task<IActionResult> Edit(int id, string? from)
    {
        var system = await _db.InfoSystems.FindAsync(id);
        if (system is null) return NotFound();
        // 記住這筆的資產編號，回到清單頁時自動帶入搜尋欄
        this.RememberSearch(system.SystemCode);
        ViewBag.From = ListSource.Resolve(from);
        return View("EditAll", system);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InfoSystem system, string? from)
    {
        if (id != system.Id) return BadRequest();
        ViewBag.From = ListSource.Resolve(from);
        await ValidateUniqueKeysAsync(system);
        if (!ModelState.IsValid) return View("EditAll", system);

        var existing = await _db.InfoSystems.FindAsync(id);
        if (existing is null) return NotFound();

        ApplySystemFields(existing, system);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新系統「{system.SeqNo} {system.SystemName}」";
        // 統一編輯畫面：存完留在原畫面，方便接著編其他區塊（from 要一起帶著，[取消]才知道回哪）
        return RedirectToAction("Edit", "Systems", new { id, from });
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

    /// <summary>資產編號與編號在主檔須唯一（資料庫也有對應的唯一索引）。</summary>
    private async Task ValidateUniqueKeysAsync(InfoSystem system)
    {
        if (!string.IsNullOrWhiteSpace(system.SystemCode)
            && await _db.InfoSystems.AnyAsync(s => s.SystemCode == system.SystemCode && s.Id != system.Id))
            ModelState.AddModelError(nameof(InfoSystem.SystemCode), $"資產編號 {system.SystemCode} 已存在");

        if (!string.IsNullOrWhiteSpace(system.SeqNo)
            && await _db.InfoSystems.AnyAsync(s => s.SeqNo == system.SeqNo && s.Id != system.Id))
            ModelState.AddModelError(nameof(InfoSystem.SeqNo), $"編號 {system.SeqNo} 已存在");
    }
}

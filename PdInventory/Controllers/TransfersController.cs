using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>Sheet2：系統自動拋轉清單</summary>
public class TransfersController : Controller
{
    private readonly AppDbContext _db;
    public TransfersController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, string? type)
    {
        var query = _db.TransferRecords.AsQueryable();

        q = this.ResolveSearch(q);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => t.SystemCode.Contains(q)
                                  || t.SystemName.Contains(q));

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(t => t.TransferType.Contains(type));

        ViewBag.Query = q;
        ViewBag.Type = type;
        return View(await query.OrderBy(t => t.SeqNo).ToListAsync());
    }

    /// <summary>匯出目前搜尋結果（含拋入/拋出篩選）。q、type 由畫面帶入，不更動搜尋記憶。</summary>
    public async Task<IActionResult> Export(string? q, string? type)
    {
        var query = _db.TransferRecords.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => t.SystemCode.Contains(q)
                                  || t.SystemName.Contains(q));
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(t => t.TransferType.Contains(type));

        var rows = await query.OrderBy(t => t.SeqNo).ToListAsync();
        var (content, fileName) = await ExcelExporter.BuildAsync(_db, "Transfers", rows);
        return File(content, ExcelExporter.ContentType, fileName);
    }

    public async Task<IActionResult> Details(int id)
    {
        var record = await _db.TransferRecords.FindAsync(id);
        if (record is null) return NotFound();
        // 記住這筆的資產編號，回到清單頁時自動帶入搜尋欄
        this.RememberSearch(record.SystemCode);
        return View(record);
    }

    public IActionResult Create() => View("Form", new TransferRecord());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransferRecord record)
    {
        await ValidateUniqueSeqNoAsync(record);
        if (!ModelState.IsValid) return View("Form", record);
        _db.TransferRecords.Add(record);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增拋轉紀錄「{record.SeqNo} {record.SystemName}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var record = await _db.TransferRecords.FindAsync(id);
        if (record is null) return NotFound();
        // 記住這筆的資產編號，回到清單頁時自動帶入搜尋欄
        this.RememberSearch(record.SystemCode);
        return View("Form", record);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TransferRecord record)
    {
        if (id != record.Id) return BadRequest();
        await ValidateUniqueSeqNoAsync(record);
        if (!ModelState.IsValid) return View("Form", record);
        _db.Update(record);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新拋轉紀錄「{record.SeqNo} {record.SystemName}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _db.TransferRecords.FindAsync(id);
        if (record is not null)
        {
            _db.TransferRecords.Remove(record);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除拋轉紀錄「{record.SeqNo} {record.SystemName}」";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>編號在主檔須唯一（資料庫也有對應的唯一索引）。</summary>
    private async Task ValidateUniqueSeqNoAsync(TransferRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.SeqNo)
            && await _db.TransferRecords.AnyAsync(t => t.SeqNo == record.SeqNo && t.Id != record.Id))
            ModelState.AddModelError(nameof(TransferRecord.SeqNo), $"編號 {record.SeqNo} 已存在");
    }
}

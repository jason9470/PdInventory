using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Controllers;

/// <summary>Sheet2：系統自動拋轉清單</summary>
public class TransfersController : Controller
{
    private readonly AppDbContext _db;
    public TransfersController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, string? type)
    {
        var query = _db.TransferRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => t.SystemCode.Contains(q)
                                  || t.SystemName.Contains(q)
                                  || t.ExternalUnit.Contains(q)
                                  || t.PathName.Contains(q)
                                  || t.Remark.Contains(q));

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(t => t.TransferType.Contains(type));

        ViewBag.Query = q;
        ViewBag.Type = type;
        return View(await query.OrderBy(t => t.SeqNo).ToListAsync());
    }

    public IActionResult Create() => View("Form", new TransferRecord());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransferRecord record)
    {
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
        return View("Form", record);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TransferRecord record)
    {
        if (id != record.Id) return BadRequest();
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
}

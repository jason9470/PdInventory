using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>資訊資產清單－資料(DA)：管理 InfoSystems 的 DA 欄位</summary>
public class DataController : Controller
{
    private readonly AppDbContext _db;
    public DataController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.InfoSystems.AsQueryable();

        q = this.ResolveSearch(q);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.DaAssetCode.Contains(q)
                                  || s.SystemCode.Contains(q));

        ViewBag.Query = q;
        return View(await query.OrderBy(s => s.SystemCode).ToListAsync());
    }

    /// <summary>匯出目前搜尋結果。q 由畫面帶入，與清單所見一致，不更動搜尋記憶。</summary>
    public async Task<IActionResult> Export(string? q)
    {
        var query = _db.InfoSystems.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.DaAssetCode.Contains(q)
                                  || s.SystemCode.Contains(q));

        var rows = await query.OrderBy(s => s.SystemCode).ToListAsync();
        var (content, fileName) = await ExcelExporter.BuildAsync(_db, "Data", rows);
        return File(content, ExcelExporter.ContentType, fileName);
    }

    // 新增與編輯畫面已整併到 Views/Shared 的 CreateAll / EditAll（由 SystemsController 提供），
    // 故此處只保留清單、編輯的 POST 與刪除；Edit 的 POST 仍是統一編輯畫面該區塊的送出目標。

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InfoSystem model, string? from)
    {
        if (id != model.Id) return BadRequest();
        ViewBag.From = ListSource.Resolve(from);
        if (!ModelState.IsValid) return View("EditAll", model);

        var existing = await _db.InfoSystems.FindAsync(id);
        if (existing is null) return NotFound();

        ApplyDataFields(existing, model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新資料資產「{existing.DaAssetCode} {existing.SystemName}」";
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
            TempData["Message"] = $"已刪除資料資產「{system.DaAssetCode} {system.SystemName}」";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 只複製 DA 欄位，保留既有 SW 與 Sheet3 資料。
    /// 共用欄位（編號／資產編號／資產名稱）改由統一編輯畫面的「系統盤點」區塊維護。
    /// </summary>
    private static void ApplyDataFields(InfoSystem t, InfoSystem s)
    {
        t.DaAssetCode = s.DaAssetCode;
        t.DaAssetType = s.DaAssetType;
        t.DaStatus = s.DaStatus;
        t.DaDescription = s.DaDescription;
        t.DaBackupMethod = s.DaBackupMethod;
        t.DaRetentionPeriod = s.DaRetentionPeriod;
        t.DaHasSensitiveData = s.DaHasSensitiveData;
        t.DaRiskOwner = s.DaRiskOwner;
        t.DaLocation = s.DaLocation;
        t.DaOwnerUnit = s.DaOwnerUnit;
        t.DaCustodianUnit = s.DaCustodianUnit;
        t.DaUserUnit = s.DaUserUnit;
        t.DaConfidentiality = s.DaConfidentiality;
        t.DaIntegrity = s.DaIntegrity;
        t.DaAvailability = s.DaAvailability;
        t.DaAssetValue = s.DaAssetValue;
        t.DaBackupConfirm = s.DaBackupConfirm;
        t.DaReviewer = s.DaReviewer;
        t.DaModifiedTime = s.DaModifiedTime;
        t.DaRemark = s.DaRemark;
    }
}

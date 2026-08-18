using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Controllers;

/// <summary>資訊資產清單－資料(DA)：管理 InfoSystems 的 DA 欄位</summary>
public class DataController : Controller
{
    private readonly AppDbContext _db;
    public DataController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.InfoSystems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.DaAssetCode.Contains(q)
                                  || s.SystemCode.Contains(q));

        ViewBag.Query = q;
        return View(await query.OrderBy(s => s.SystemCode).ToListAsync());
    }

    public IActionResult Create() => View("Form", new InfoSystem());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InfoSystem model)
    {
        if (!ModelState.IsValid) return View("Form", model);
        _db.InfoSystems.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增資料資產「{model.DaAssetCode} {model.SystemName}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var system = await _db.InfoSystems.FindAsync(id);
        if (system is null) return NotFound();
        return View("Form", system);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InfoSystem model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View("Form", model);

        var existing = await _db.InfoSystems.FindAsync(id);
        if (existing is null) return NotFound();

        ApplyDataFields(existing, model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新資料資產「{existing.DaAssetCode} {existing.SystemName}」";
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
            TempData["Message"] = $"已刪除資料資產「{system.DaAssetCode} {system.SystemName}」";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>只複製系統基本欄位與 DA 欄位，保留既有 SW 與 Sheet3 資料。</summary>
    private static void ApplyDataFields(InfoSystem t, InfoSystem s)
    {
        t.SeqNo = s.SeqNo;
        t.SystemCode = s.SystemCode;
        t.SystemName = s.SystemName;

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

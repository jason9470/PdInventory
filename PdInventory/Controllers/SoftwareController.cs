using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>資訊資產清單－軟體(SW)：管理 InfoSystems 的 SW 欄位</summary>
public class SoftwareController : Controller
{
    private readonly AppDbContext _db;
    public SoftwareController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.InfoSystems.AsQueryable();

        q = this.ResolveSearch(q);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.SystemCode.Contains(q)
                                  || s.SystemName.Contains(q));

        ViewBag.Query = q;
        return View(await query.OrderBy(s => s.SystemCode).ToListAsync());
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

        ApplySoftwareFields(existing, model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新軟體資產「{existing.SystemCode} {existing.SystemName}」";
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
            TempData["Message"] = $"已刪除軟體資產「{system.SystemCode} {system.SystemName}」";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 只複製 SW 欄位，保留既有 DA 與 Sheet3 資料。
    /// 共用欄位（編號／資產編號／資產名稱）與資產說明改由統一編輯畫面的
    /// 「系統盤點」「DA」區塊維護，此處不寫入，以免用舊值蓋掉別的區塊剛存的修改。
    /// </summary>
    private static void ApplySoftwareFields(InfoSystem t, InfoSystem s)
    {
        t.SwStatus = s.SwStatus;
        t.SwAssetType = s.SwAssetType;
        t.SwSystemCategory = s.SwSystemCategory;
        t.SwAdIntegration = s.SwAdIntegration;
        t.SwOsVersion = s.SwOsVersion;
        t.SwDbToolVersion = s.SwDbToolVersion;
        t.SwThirdPartyComponents = s.SwThirdPartyComponents;
        t.SwUserAccountGrant = s.SwUserAccountGrant;
        t.SwProvidesAccountReport = s.SwProvidesAccountReport;
        t.SwRiskOwner = s.SwRiskOwner;
        t.SwLocation = s.SwLocation;
        t.SwOwnerUnit = s.SwOwnerUnit;
        t.SwCustodianUnit = s.SwCustodianUnit;
        t.SwUserUnit = s.SwUserUnit;
        t.SwConfidentiality = s.SwConfidentiality;
        t.SwIntegrity = s.SwIntegrity;
        t.SwAvailability = s.SwAvailability;
        t.SwAssetValue = s.SwAssetValue;
        t.SwBusinessContact = s.SwBusinessContact;
        t.SwAppManager = s.SwAppManager;
        t.SwAppMaintainer = s.SwAppMaintainer;
        t.SwAppMaintainerDeputy = s.SwAppMaintainerDeputy;
        t.SwOperator = s.SwOperator;
        t.SwDevMode = s.SwDevMode;
        t.SwMaintMode = s.SwMaintMode;
        t.SwVendor = s.SwVendor;
        t.SwLanguage = s.SwLanguage;
        t.SwVersionControl = s.SwVersionControl;
        t.SwApRepoPath = s.SwApRepoPath;
        t.SwDeployMethod = s.SwDeployMethod;
        t.SwOpRepoPath = s.SwOpRepoPath;
        t.SwCodeAccess = s.SwCodeAccess;
        t.SwDeveloper = s.SwDeveloper;
        t.SwDeployer = s.SwDeployer;
        t.SwRpo = s.SwRpo;
        t.SwLocalBackup = s.SwLocalBackup;
        t.SwLocalBackupType = s.SwLocalBackupType;
        t.SwLocalBackupFreq = s.SwLocalBackupFreq;
        t.SwRemoteBackup = s.SwRemoteBackup;
        t.SwRemoteBackupType = s.SwRemoteBackupType;
        t.SwRemoteBackupFreq = s.SwRemoteBackupFreq;
        t.SwLocalHa = s.SwLocalHa;
        t.SwLocalHaArch = s.SwLocalHaArch;
        t.SwRemoteHa = s.SwRemoteHa;
        t.SwRemoteHaArch = s.SwRemoteHaArch;
        t.SwRto = s.SwRto;
        t.SwHasRecoveryPlan = s.SwHasRecoveryPlan;
        t.SwHasDrDrill = s.SwHasDrDrill;
        t.SwRelatedSystems = s.SwRelatedSystems;
        t.SwHandlesPersonalData = s.SwHandlesPersonalData;
        t.SwHasUiAuth = s.SwHasUiAuth;
        t.SwKeepsPdTrail = s.SwKeepsPdTrail;
        t.SwProvidesApi = s.SwProvidesApi;
        t.SwTrailLocation = s.SwTrailLocation;
        t.SwTrailStorage = s.SwTrailStorage;
        t.SwBusinessOwnerUnit = s.SwBusinessOwnerUnit;
        t.SwIsCoreSystem = s.SwIsCoreSystem;
        t.SwReviewer = s.SwReviewer;
        t.SwModifiedBy = s.SwModifiedBy;
        t.SwModifiedTime = s.SwModifiedTime;
        t.SwRemark = s.SwRemark;
    }
}

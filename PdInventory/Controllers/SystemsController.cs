using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Models.ViewModels;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>
/// 資訊系統、資料庫與檔案伺服器盤點表。
///
/// 盤點表已獨立成 SystemInventories 資料表，但依業務端確認一定依附於某個軟體資產，
/// 因此資產編號必填。本控制器同時提供 SW／DA／盤點表的統一新增與編輯畫面。
/// </summary>
[Authorize(Policy = Policies.ViewAssets)]
public class SystemsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAssetAccess _access;

    public SystemsController(AppDbContext db, ICurrentUser currentUser, IAssetAccess access)
    {
        _db = db;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task<IActionResult> Index(string? q)
    {
        q = this.ResolveSearch(q);
        var rows = await FilteredAsync(q);

        ViewBag.Query = q;
        await LoadAssetLookupAsync(rows);
        return View(rows);
    }

    /// <summary>匯出目前搜尋結果。q 由畫面帶入，與清單所見一致，不更動搜尋記憶。</summary>
    public async Task<IActionResult> Export(string? q)
    {
        var rows = await FilteredAsync(q);
        var (content, fileName) = await ExcelExporter.BuildAsync(_db, "Systems", rows);
        return File(content, ExcelExporter.ContentType, fileName);
    }

    private async Task<List<SystemInventory>> FilteredAsync(string? q)
    {
        var query = _db.SystemInventories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(i => i.SystemCode.Contains(q) || i.DbName.Contains(q));

        return await query.OrderBy(i => i.SystemCode).ToListAsync();
    }

    /// <summary>編號與資產名稱屬於軟體資產那一邊，清單要顯示就得補查。</summary>
    private async Task LoadAssetLookupAsync(List<SystemInventory> rows)
    {
        var codes = rows.Select(i => i.SystemCode).Distinct().ToList();
        ViewBag.Assets = await _db.InfoSystems
            .Where(s => codes.Contains(s.SystemCode))
            .ToDictionaryAsync(s => s.SystemCode);
        // [檢視]與[編輯]連到統一畫面，而它的主鍵是 DataAssets.Id
        ViewBag.DataAssetIds = await AssetGroups.DataAssetIdsAsync(_db, codes);
    }

    // 新增、編輯與檢視畫面都整併在 DataController（統一畫面的主鍵是 DataAssets.Id），
    // 故此處只保留清單、盤點表區塊的送出目標與刪除。

    /// <param name="id">DataAssets 的主鍵，也是統一編輯畫面的網址參數。</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Sheet3")] SystemEditViewModel model, string? from)
    {
        ViewBag.From = ListSource.Resolve(from);

        var group = await AssetGroups.LoadAsync(_db, id);
        // 沒有關連 SW 的資料資產不會顯示盤點表區塊，也沒有資產編號可以掛盤點表
        if (group?.System is null) return NotFound();

        var system = group.System;

        if (!await _access.CanModifyAsync(system.SystemCode)) return Forbid();

        if (!ModelState.IsValid)
        {
            var reload = InfoSystemBlocks.ToEditViewModel(group);
            reload.Sheet3 = model;
            return View("EditAll", reload);
        }

        // 這一列從主鍵找回來，不看表單送來的 Id：表單的 Id 只是畫面上的回填值，
        // 拿它去 FindAsync 撞到別張表的主鍵時會安靜地建出一列新的盤點表。
        var existing = group.Inventory;
        if (existing is null)
        {
            // 這套系統原本沒有盤點表（55 套裡有 29 套是這樣），存檔時才建出那一列
            existing = new SystemInventory();
            _db.SystemInventories.Add(existing);
        }
        else
        {
            // 以畫面載入當下的權杖比對：期間內被別人存過就擋下，不做靜默覆蓋
            _db.Entry(existing).Property(e => e.RowVersion).OriginalValue = model.RowVersion;
        }

        InfoSystemBlocks.Copy(existing, model);
        existing.SystemCode = system.SystemCode;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "這筆資料在你編輯期間已被其他人修改，畫面已重新載入最新內容，請確認後再存一次。";
            return RedirectToAction("Edit", "Data", new { id, from });
        }

        TempData["Message"] = $"已更新系統盤點「{system.SystemCode} {system.SystemName}」";
        return RedirectToAction("Edit", "Data", new { id, from });
    }

    /// <summary>
    /// 只刪盤點表這一列，SW 與 DA 留著（業務端 0910 確認）。這裡的刪除是
    /// 「這套系統不需要盤點表」的意思，55 套系統裡本來就有 29 套沒有這一列，
    /// 刪掉只是回到那個狀態。整筆資產不要了要從 SW 清單或 DA 清單刪。
    /// </summary>
    /// <param name="id">SystemInventories 的主鍵。</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var inventory = await _db.SystemInventories.FindAsync(id);
        if (inventory is not null)
        {
            // 刪除只開放給管理者（0917）：修改權限依科別開放給所有人之後，
            // 連帶開放刪除的風險太大。清單頁不會顯示按鈕，直接輸入網址也擋下
            if (!_access.CanDelete) return Forbid();

            // 軟刪除：只加註記，全域查詢篩選讓它從清單、檢視與匯出中消失
            inventory.IsDeleted = true;
            inventory.DeletedAt = DateTime.Now;
            inventory.DeletedBy = _currentUser.Name;
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除系統盤點「{inventory.SystemCode}」";
        }
        return RedirectToAction(nameof(Index));
    }

}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Models.ViewModels;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>資訊資產清單－軟體(SW)：管理 InfoSystems 的 SW 欄位</summary>
[Authorize(Policy = Policies.ViewAssets)]
public class SoftwareController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAssetAccess _access;

    public SoftwareController(AppDbContext db, ICurrentUser currentUser, IAssetAccess access)
    {
        _db = db;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.InfoSystems.AsQueryable();

        q = this.ResolveSearch(q);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.SystemCode.Contains(q)
                                  || s.SystemName.Contains(q));

        var rows = await query.OrderBy(s => s.SystemCode).ToListAsync();

        ViewBag.Query = q;
        // [檢視]與[編輯]連到統一畫面，而它的主鍵是 DataAssets.Id
        ViewBag.DataAssetIds = await AssetGroups.DataAssetIdsAsync(_db, rows.Select(s => s.SystemCode));
        return View(rows);
    }

    /// <summary>匯出目前搜尋結果。q 由畫面帶入，與清單所見一致，不更動搜尋記憶。</summary>
    public async Task<IActionResult> Export(string? q)
    {
        var query = _db.InfoSystems.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.SystemCode.Contains(q)
                                  || s.SystemName.Contains(q));

        var rows = await query.OrderBy(s => s.SystemCode).ToListAsync();
        var (content, fileName) = await ExcelExporter.BuildAsync(_db, "Software", rows);
        return File(content, ExcelExporter.ContentType, fileName);
    }

    // 新增、編輯與檢視畫面都整併在 DataController（統一畫面的主鍵是 DataAssets.Id），
    // 故此處只保留清單、SW 區塊的送出目標與刪除。

    /// <param name="id">DataAssets 的主鍵，也是統一編輯畫面的網址參數。</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Software")] SoftwareEditViewModel model, string? from)
    {
        ViewBag.From = ListSource.Resolve(from);

        var group = await AssetGroups.LoadAsync(_db, id);
        // 沒有關連 SW 的資料資產根本不會顯示這個區塊，送過來就是不該發生的事
        if (group?.System is null) return NotFound();

        var existing = group.System;

        // 只能異動自己科別負責的資產；清單頁雖然不會顯示按鈕，直接輸入網址仍必須擋下
        if (!await _access.CanModifyAsync(existing.SystemCode)) return Forbid();

        if (!ModelState.IsValid)
        {
            // 統一編輯畫面要三個區塊都在，另外兩塊取資料庫現值，這一塊保留使用者剛才輸入的內容
            var reload = InfoSystemBlocks.ToEditViewModel(group);
            reload.Software = model;
            return View("EditAll", reload);
        }

        // 編號／資產編號／資產名稱在編輯畫面是唯讀的，一律以資料庫現值為準。
        // readonly 只是操作防呆，改個表單欄位就繞過去了——而資產編號串起六張表、
        // 又是權限判斷的依據，在這裡被改掉會讓那些關聯瞬間斷開。
        model.SeqNo = existing.SeqNo;
        model.SystemCode = existing.SystemCode;
        model.SystemName = existing.SystemName;

        InfoSystemBlocks.Copy(existing, model);
        // 以畫面載入當下的權杖比對：若這筆在期間內被他人存過，擋下並要求重新載入，
        // 不做靜默覆蓋。權杖由 AppDbContext 於每次存檔換新。
        _db.Entry(existing).Property(e => e.RowVersion).OriginalValue = model.RowVersion;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "這筆資料在你編輯期間已被其他人修改，畫面已重新載入最新內容，請確認後再存一次。";
            return RedirectToAction("Edit", "Data", new { id, from });
        }
        TempData["Message"] = $"已更新軟體資產「{existing.SystemCode} {existing.SystemName}」";
        // 統一編輯畫面：存完留在原畫面，方便接著編其他區塊（from 要一起帶著，[取消]才知道回哪）
        return RedirectToAction("Edit", "Data", new { id, from });
    }

    /// <summary>
    /// SW／DA／盤點表三列一起軟刪除。三張清單是同一筆資產的三個面，從這裡刪掉它
    /// 就是整筆不要了（業務端 0910 確認）。只刪 SW 也不可行：DA 會變成指向一個
    /// 已刪除的資產編號，統一編輯畫面會顯示一個不存在的 SW 區塊。
    ///
    /// 軟刪除：只加註記，資料仍留在資料庫，但被全域查詢篩選排除，
    /// 因此三張清單與匯出都不會再出現。
    /// </summary>
    /// <param name="id">InfoSystems 的主鍵（這是 SW 清單，列的就是 InfoSystems）。</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var system = await _db.InfoSystems.FindAsync(id);
        if (system is null) return RedirectToAction(nameof(Index));

        // 刪除只開放給管理者（0917）：這裡刪的是整筆資產連同 DA 與盤點表，
        // 修改權限依科別開放給所有人之後，連帶開放刪除的風險太大
        if (!_access.CanDelete) return Forbid();

        var data = await _db.DataAssets.FirstOrDefaultAsync(d => d.SystemCode == system.SystemCode);
        var inventory = await _db.SystemInventories.FirstOrDefaultAsync(i => i.SystemCode == system.SystemCode);

        // 每個 SW 都配一列 DA，但真的缺了也不該炸掉刪除動作——沒有就只刪剩下的
        if (data is not null)
        {
            AssetGroups.SoftDelete(new AssetGroup(data, system, inventory), _currentUser.Name);
        }
        else
        {
            system.IsDeleted = true;
            system.DeletedAt = DateTime.Now;
            system.DeletedBy = _currentUser.Name;
        }

        await _db.SaveChangesAsync();
        TempData["Message"] = $"已刪除資訊資產「{system.SystemCode} {system.SystemName}」的 SW、DA 與盤點表資料";
        return RedirectToAction(nameof(Index));
    }
}

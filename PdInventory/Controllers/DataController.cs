using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Models.ViewModels;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>
/// 資訊資產清單－資料(DA)。
///
/// DA 已獨立成 DataAssets 資料表，可以沒有對應的軟體資產（終端設備類的資料資產就是），
/// 因此這裡查的是 DataAssets，而不是 InfoSystems。
/// </summary>
[Authorize(Policy = Policies.ViewAssets)]
public class DataController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAssetAccess _access;

    public DataController(AppDbContext db, ICurrentUser currentUser, IAssetAccess access)
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
        var (content, fileName) = await ExcelExporter.BuildAsync(_db, "Data", rows);
        return File(content, ExcelExporter.ContentType, fileName);
    }

    private async Task<List<DataAsset>> FilteredAsync(string? q)
    {
        var query = _db.DataAssets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(d => d.DaAssetCode.Contains(q) || d.SystemCode.Contains(q));

        return await query.OrderBy(d => d.DaAssetCode).ToListAsync();
    }

    /// <summary>
    /// 資產狀態與資產名稱屬於軟體資產那一邊，清單要顯示就得補查。
    /// 沒有關連 SW 的資料資產（例如各組的 PC／NB）查不到，畫面留空即可。
    /// </summary>
    private async Task LoadAssetLookupAsync(List<DataAsset> rows)
    {
        var codes = rows.Select(d => d.SystemCode)
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Distinct()
                        .ToList();

        ViewBag.Assets = await _db.InfoSystems
            .Where(s => codes.Contains(s.SystemCode))
            .ToDictionaryAsync(s => s.SystemCode);
    }

    // 新增與編輯畫面仍整併在 Views/Shared 的 CreateAll / EditAll（由 SystemsController 提供），
    // 故此處只保留清單、編輯的 POST 與刪除。

    /// <param name="id">統一編輯畫面所在的 InfoSystems 主鍵；DataAssets 的主鍵在 model.Id。</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Data")] DataEditViewModel model, string? from)
    {
        ViewBag.From = ListSource.Resolve(from);

        var system = await _db.InfoSystems.FindAsync(id);
        if (system is null) return NotFound();

        // 資產負責人只能異動名下的資產；清單頁雖然不會顯示按鈕，直接輸入網址仍必須擋下
        if (!await _access.CanModifyAsync(system.SystemCode)) return Forbid();

        if (!ModelState.IsValid)
        {
            var reload = await BuildEditModelAsync(system);
            reload.Data = model;
            return View("EditAll", reload);
        }

        // 這個軟體資產原本沒有 DA 資料時，存檔就替它建一列
        var existing = model.Id > 0 ? await _db.DataAssets.FindAsync(model.Id) : null;
        if (existing is null)
        {
            existing = new DataAsset();
            _db.DataAssets.Add(existing);
        }
        else
        {
            // 以畫面載入當下的權杖比對：若這筆在期間內被他人存過，擋下並要求重新載入，
            // 不做靜默覆蓋。權杖由 AppDbContext 於每次存檔換新。
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
            return RedirectToAction("Edit", "Systems", new { id, from });
        }

        TempData["Message"] = $"已更新資料資產「{existing.DaAssetCode} {system.SystemName}」";
        // 統一編輯畫面：存完留在原畫面，方便接著編其他區塊（from 要一起帶著，[取消]才知道回哪）
        return RedirectToAction("Edit", "Systems", new { id, from });
    }

    /// <param name="id">DataAssets 的主鍵。</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var asset = await _db.DataAssets.FindAsync(id);
        if (asset is not null)
        {
            // 沒有關連 SW 的資料資產無人可認領，只有主管以上能刪
            if (!await _access.CanModifyAsync(asset.SystemCode)) return Forbid();

            // 軟刪除：只加註記，全域查詢篩選讓它從清單、檢視與匯出中消失
            asset.IsDeleted = true;
            asset.DeletedAt = DateTime.Now;
            asset.DeletedBy = _currentUser.Name;
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除資料資產「{asset.DaAssetCode}」";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<InfoSystemEditViewModel> BuildEditModelAsync(InfoSystem system)
    {
        var dataAsset = await _db.DataAssets
            .FirstOrDefaultAsync(d => d.SystemCode == system.SystemCode);
        var inventory = await _db.SystemInventories
            .FirstOrDefaultAsync(i => i.SystemCode == system.SystemCode);

        return InfoSystemBlocks.ToEditViewModel(system, dataAsset, inventory);
    }
}

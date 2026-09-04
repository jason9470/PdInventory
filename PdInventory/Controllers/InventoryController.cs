using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Models.ViewModels;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>Sheet1：個人資料檔案盤點表</summary>
[Authorize(Policy = Policies.ViewAssets)]
public class InventoryController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAssetAccess _access;
    private readonly ICurrentUser _currentUser;

    public InventoryController(AppDbContext db, IAssetAccess access, ICurrentUser currentUser)
    {
        _db = db;
        _access = access;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.InventoryItems
            .Include(i => i.Categories)
            .Include(i => i.Purposes)
            .AsQueryable();

        q = this.ResolveSearch(q);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(i => i.DocumentName.Contains(q)
                                  || i.SystemCode.Contains(q)
                                  || i.SystemName.Contains(q));

        ViewBag.Query = q;
        return View(await query.OrderBy(i => i.SeqNo).ToListAsync());
    }

    /// <summary>匯出目前搜尋結果。q 由畫面帶入，與清單所見一致，不更動搜尋記憶。</summary>
    public async Task<IActionResult> Export(string? q)
    {
        var query = _db.InventoryItems
            .Include(i => i.Categories)
            .Include(i => i.Purposes)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(i => i.DocumentName.Contains(q)
                                  || i.SystemCode.Contains(q)
                                  || i.SystemName.Contains(q));

        var rows = await query.OrderBy(i => i.SeqNo).ToListAsync();
        var (content, fileName) = await ExcelExporter.BuildAsync(_db, "Inventory", rows);
        return File(content, ExcelExporter.ContentType, fileName);
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Categories)
            .Include(i => i.Purposes)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return NotFound();
        // 記住這筆的資產編號，回到清單頁時自動帶入搜尋欄
        this.RememberSearch(item.SystemCode);
        return View(item);
    }

    [Authorize(Policy = Policies.ManageAssets)]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View("Form", new InventoryItemEditViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageAssets)]
    public async Task<IActionResult> Create(InventoryItemEditViewModel model,
                                            int[] categoryIds, int[] purposeIds)
    {
        await ValidateUniqueSeqNoAsync(model.Id, model.SeqNo);
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(categoryIds, purposeIds);
            return View("Form", model);
        }

        // 名稱一律依編號查出，不採信畫面送回來的值
        model.SystemName = await AssetPicker.ResolveNameAsync(_db, model.SystemCode) ?? model.SystemName;

        var item = new InventoryItem();
        InfoSystemBlocks.Copy(item, model);
        item.Categories = await _db.Categories.Where(c => categoryIds.Contains(c.Id)).ToListAsync();
        item.Purposes = await _db.Purposes.Where(p => purposeIds.Contains(p.Id)).ToListAsync();
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"已新增盤點項目「{item.SeqNo} {item.DocumentName}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Categories)
            .Include(i => i.Purposes)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return NotFound();

        // 資產負責人只能異動名下資產底下的資料；清單頁雖然不會顯示按鈕，直接輸入網址仍必須擋下
        if (!await _access.CanModifyAsync(item.SystemCode)) return Forbid();

        // 記住這筆的資產編號，回到清單頁時自動帶入搜尋欄
        this.RememberSearch(item.SystemCode);
        await LoadLookupsAsync(
            item.Categories.Select(c => c.Id).ToArray(),
            item.Purposes.Select(p => p.Id).ToArray());

        var model = new InventoryItemEditViewModel();
        InfoSystemBlocks.Copy(model, item);
        model.Id = item.Id;
        model.RowVersion = item.RowVersion;
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InventoryItemEditViewModel model,
                                          int[] categoryIds, int[] purposeIds)
    {
        if (id != model.Id) return BadRequest();

        var existing = await _db.InventoryItems
            .Include(i => i.Categories)
            .Include(i => i.Purposes)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (existing is null) return NotFound();

        // 原資產與改後的資產都必須在權限範圍內，否則資產負責人可以把不屬於自己的資料
        // 改成自己的資產編號、或把自己的資料丟給別人
        if (!await _access.CanModifyAsync(existing.SystemCode)
            || !await _access.CanModifyAsync(model.SystemCode)) return Forbid();

        await ValidateUniqueSeqNoAsync(model.Id, model.SeqNo);
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(categoryIds, purposeIds);
            return View("Form", model);
        }

        model.SystemName = await AssetPicker.ResolveNameAsync(_db, model.SystemCode) ?? model.SystemName;

        // 只搬 ViewModel 上有的欄位。風險自評那一組、軌跡與刪除註記都不在上面，
        // 因此不必再手動回填既有值——它們根本不會被畫面覆寫。
        InfoSystemBlocks.Copy(existing, model);
        existing.Categories = await _db.Categories.Where(c => categoryIds.Contains(c.Id)).ToListAsync();
        existing.Purposes = await _db.Purposes.Where(p => purposeIds.Contains(p.Id)).ToListAsync();
        _db.Entry(existing).Property(e => e.RowVersion).OriginalValue = model.RowVersion;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "這筆資料在你編輯期間已被其他人修改，畫面已重新載入最新內容，請確認後再存一次。";
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["Message"] = $"已更新盤點項目「{existing.SeqNo} {existing.DocumentName}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item is not null)
        {
            // 資產負責人只能刪除名下資產底下的資料
            if (!await _access.CanModifyAsync(item.SystemCode)) return Forbid();

            // 軟刪除：只加註記，全域查詢篩選讓它從清單、檢視與匯出中消失
            item.IsDeleted = true;
            item.DeletedAt = DateTime.Now;
            item.DeletedBy = _currentUser.Name;
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除盤點項目「{item.SeqNo} {item.DocumentName}」";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookupsAsync(int[]? selectedCategoryIds = null, int[]? selectedPurposeIds = null)
    {
        ViewBag.AllCategories = await _db.Categories
            .OrderBy(c => c.Code).ToListAsync();
        ViewBag.AllPurposes = await _db.Purposes
            .OrderBy(p => p.Code).ToListAsync();
        ViewBag.AssetOptions = await AssetPicker.LoadAsync(_db);
        ViewBag.SelectedCategoryIds = selectedCategoryIds ?? Array.Empty<int>();
        ViewBag.SelectedPurposeIds = selectedPurposeIds ?? Array.Empty<int>();
    }

    /// <summary>編號在主檔須唯一（資料庫也有對應的唯一索引）。</summary>
    private async Task ValidateUniqueSeqNoAsync(int id, string seqNo)
    {
        if (!string.IsNullOrWhiteSpace(seqNo)
            && await _db.InventoryItems.AnyAsync(i => i.SeqNo == seqNo && i.Id != id))
            ModelState.AddModelError(nameof(InventoryItem.SeqNo), $"編號 {seqNo} 已存在");
    }
}

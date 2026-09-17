using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Models.ViewModels;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>Sheet2：系統自動拋轉清單</summary>
[Authorize(Policy = Policies.ViewAssets)]
public class TransfersController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAssetAccess _access;
    private readonly ICurrentUser _currentUser;

    public TransfersController(AppDbContext db, IAssetAccess access, ICurrentUser currentUser)
    {
        _db = db;
        _access = access;
        _currentUser = currentUser;
    }

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

    [Authorize(Policy = Policies.CreateAssets)]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View("Form", new TransferRecordEditViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CreateAssets)]
    public async Task<IActionResult> Create(TransferRecordEditViewModel model)
    {
        await ValidateUniqueSeqNoAsync(model.Id, model.SeqNo);
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View("Form", model);
        }

        // 名稱一律依編號查出，不採信畫面送回來的值
        model.SystemName = await AssetPicker.ResolveNameAsync(_db, model.SystemCode) ?? model.SystemName;

        var record = new TransferRecord();
        InfoSystemBlocks.Copy(record, model);
        _db.TransferRecords.Add(record);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"已新增拋轉紀錄「{record.SeqNo} {record.SystemName}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var record = await _db.TransferRecords.FindAsync(id);
        if (record is null) return NotFound();

        // 只能異動自己科別負責的資產底下的資料；直接輸入網址也必須擋下
        if (!await _access.CanModifyAsync(record.SystemCode)) return Forbid();

        // 記住這筆的資產編號，回到清單頁時自動帶入搜尋欄
        this.RememberSearch(record.SystemCode);
        await LoadLookupsAsync();

        var model = new TransferRecordEditViewModel();
        InfoSystemBlocks.Copy(model, record);
        model.Id = record.Id;
        model.RowVersion = record.RowVersion;
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TransferRecordEditViewModel model)
    {
        if (id != model.Id) return BadRequest();

        var existing = await _db.TransferRecords.FirstOrDefaultAsync(t => t.Id == id);
        if (existing is null) return NotFound();

        // 原資產與改後的資產都必須在權限範圍內，否則使用者可以把不屬於自己科別的資料
        // 改成自己的資產編號、或把自己的資料丟給別人
        if (!await _access.CanModifyAsync(existing.SystemCode)
            || !await _access.CanModifyAsync(model.SystemCode)) return Forbid();

        await ValidateUniqueSeqNoAsync(model.Id, model.SeqNo);
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View("Form", model);
        }

        model.SystemName = await AssetPicker.ResolveNameAsync(_db, model.SystemCode) ?? model.SystemName;

        // 只搬 ViewModel 上有的欄位：軌跡與刪除註記不在上面，不會被畫面覆寫
        InfoSystemBlocks.Copy(existing, model);
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

        TempData["Message"] = $"已更新拋轉紀錄「{existing.SeqNo} {existing.SystemName}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _db.TransferRecords.FindAsync(id);
        if (record is not null)
        {
            // 刪除只開放給管理者（0917）：修改權限依科別開放給所有人之後，
            // 連帶開放刪除的風險太大。清單頁不會顯示按鈕，直接輸入網址也擋下
            if (!_access.CanDelete) return Forbid();

            // 軟刪除：只加註記，全域查詢篩選讓它從清單、檢視與匯出中消失
            record.IsDeleted = true;
            record.DeletedAt = DateTime.Now;
            record.DeletedBy = _currentUser.Name;
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除拋轉紀錄「{record.SeqNo} {record.SystemName}」";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>資產編號下拉的選項。</summary>
    private async Task LoadLookupsAsync() =>
        ViewBag.AssetOptions = await AssetPicker.LoadAsync(_db);

    /// <summary>編號在主檔須唯一（資料庫也有對應的唯一索引）。</summary>
    private async Task ValidateUniqueSeqNoAsync(int id, string seqNo)
    {
        if (!string.IsNullOrWhiteSpace(seqNo)
            && await _db.TransferRecords.AnyAsync(t => t.SeqNo == seqNo && t.Id != id))
            ModelState.AddModelError(nameof(TransferRecord.SeqNo), $"編號 {seqNo} 已存在");
    }
}

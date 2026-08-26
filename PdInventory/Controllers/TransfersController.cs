using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>Sheet2：系統自動拋轉清單</summary>
[Authorize(Policy = Policies.ViewAssets)]
public class TransfersController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAssetAccess _access;

    public TransfersController(AppDbContext db, IAssetAccess access)
    {
        _db = db;
        _access = access;
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

    [Authorize(Policy = Policies.ManageAssets)]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View("Form", new TransferRecord());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageAssets)]
    public async Task<IActionResult> Create(TransferRecord record)
    {
        await ValidateUniqueSeqNoAsync(record);
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View("Form", record);
        }

        // 名稱一律依編號查出，不採信畫面送回來的值
        record.SystemName = await AssetPicker.ResolveNameAsync(_db, record.SystemCode) ?? record.SystemName;
        _db.TransferRecords.Add(record);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增拋轉紀錄「{record.SeqNo} {record.SystemName}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var record = await _db.TransferRecords.FindAsync(id);
        if (record is null) return NotFound();

        // 資產負責人只能異動名下資產底下的資料；直接輸入網址也必須擋下
        if (!await _access.CanModifyAsync(record.SystemCode)) return Forbid();

        // 記住這筆的資產編號，回到清單頁時自動帶入搜尋欄
        this.RememberSearch(record.SystemCode);
        await LoadLookupsAsync();
        return View("Form", record);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TransferRecord record)
    {
        if (id != record.Id) return BadRequest();

        // 不追蹤讀取：後面的 _db.Update(record) 會把送進來的物件掛上追蹤器，
        // 這裡若用追蹤查詢會與它衝突。原資產與改後的資產都必須在權限範圍內。
        var currentCode = await _db.TransferRecords.AsNoTracking()
            .Where(t => t.Id == id).Select(t => t.SystemCode).FirstOrDefaultAsync();
        if (currentCode is null) return NotFound();
        if (!await _access.CanModifyAsync(currentCode)
            || !await _access.CanModifyAsync(record.SystemCode)) return Forbid();

        await ValidateUniqueSeqNoAsync(record);
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View("Form", record);
        }

        record.SystemName = await AssetPicker.ResolveNameAsync(_db, record.SystemCode) ?? record.SystemName;
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
            // 資產負責人只能刪除名下資產底下的拋轉紀錄
            if (!await _access.CanModifyAsync(record.SystemCode)) return Forbid();

            _db.TransferRecords.Remove(record);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除拋轉紀錄「{record.SeqNo} {record.SystemName}」";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>資產編號下拉的選項。</summary>
    private async Task LoadLookupsAsync() =>
        ViewBag.AssetOptions = await AssetPicker.LoadAsync(_db);

    /// <summary>編號在主檔須唯一（資料庫也有對應的唯一索引）。</summary>
    private async Task ValidateUniqueSeqNoAsync(TransferRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.SeqNo)
            && await _db.TransferRecords.AnyAsync(t => t.SeqNo == record.SeqNo && t.Id != record.Id))
            ModelState.AddModelError(nameof(TransferRecord.SeqNo), $"編號 {record.SeqNo} 已存在");
    }
}

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
    private readonly ICurrentUser _currentUser;

    public DataController(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

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
            return RedirectToAction("Edit", "Systems", new { id, from });
        }
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
            // 軟刪除：只加註記，資料仍留在資料庫，但被全域查詢篩選排除，
            // 因此 SW／DA／系統盤點三張清單與匯出都不會再出現。
            system.IsDeleted = true;
            system.DeletedAt = DateTime.Now;
            system.DeletedBy = _currentUser.Name;
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除資料資產「{system.DaAssetCode} {system.SystemName}」";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>只複製 DA 欄位，保留既有 SW 與 Sheet3 資料。</summary>
    private static void ApplyDataFields(InfoSystem t, InfoSystem s) =>
        InfoSystemBlocks.Copy(t, s, InfoSystemBlocks.Da);
}

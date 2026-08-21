using Microsoft.AspNetCore.Mvc;
using PdInventory.Data;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>維護匯出：調整各清單匯出 Excel 的欄位順序與是否納入。</summary>
public class ExportSettingsController : Controller
{
    private readonly AppDbContext _db;
    public ExportSettingsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Edit(string list)
    {
        if (!ExportCatalog.IsKnown(list)) return NotFound();

        ViewBag.ListKey = list;
        ViewBag.ListTitle = ExportCatalog.Lists[list].Title;
        return View(await ExportCatalog.ResolveAsync(_db, list));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string list, string[] propertyNames, string[] included)
    {
        if (!ExportCatalog.IsKnown(list)) return NotFound();

        await ExportCatalog.SaveAsync(_db, list, propertyNames ?? [], included ?? []);
        TempData["Message"] = $"已儲存「{ExportCatalog.Lists[list].Title}」的匯出欄位設定";
        return RedirectToAction(nameof(Edit), new { list });
    }

    /// <summary>還原成模型的預設順序（清掉該清單的設定）。</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(string list)
    {
        if (!ExportCatalog.IsKnown(list)) return NotFound();

        var existing = _db.ExportColumns.Where(c => c.ListKey == list);
        _db.ExportColumns.RemoveRange(existing);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"已還原「{ExportCatalog.Lists[list].Title}」的預設欄位順序";
        return RedirectToAction(nameof(Edit), new { list });
    }
}

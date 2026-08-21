using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>Sheet1：個人資料檔案盤點表</summary>
public class InventoryController : Controller
{
    private readonly AppDbContext _db;
    public InventoryController(AppDbContext db) => _db = db;

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

    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View("Form", new InventoryItem());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryItem item, int[] categoryIds, int[] purposeIds)
    {
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(categoryIds, purposeIds);
            return View("Form", item);
        }

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

        // 記住這筆的資產編號，回到清單頁時自動帶入搜尋欄
        this.RememberSearch(item.SystemCode);
        await LoadLookupsAsync(
            item.Categories.Select(c => c.Id).ToArray(),
            item.Purposes.Select(p => p.Id).ToArray());
        return View("Form", item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InventoryItem item, int[] categoryIds, int[] purposeIds)
    {
        if (id != item.Id) return BadRequest();

        var existing = await _db.InventoryItems
            .Include(i => i.Categories)
            .Include(i => i.Purposes)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (existing is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(categoryIds, purposeIds);
            return View("Form", item);
        }

        // 本表單不維護風險自評欄位，先帶回既有值，避免 SetValues 以空值覆寫。
        item.RiskDataSeqNo = existing.RiskDataSeqNo;
        item.RiskCategoryCode = existing.RiskCategoryCode;
        item.RiskCategoryName = existing.RiskCategoryName;
        item.RiskEvent = existing.RiskEvent;
        item.RiskImpactLevel = existing.RiskImpactLevel;
        item.RiskLikelihoodLevel = existing.RiskLikelihoodLevel;
        item.RiskRelatedRegulation = existing.RiskRelatedRegulation;
        item.RiskControlDescription = existing.RiskControlDescription;
        item.RiskEffectivenessLevel = existing.RiskEffectivenessLevel;
        item.RiskValue = existing.RiskValue;
        item.RiskImprovementPlan = existing.RiskImprovementPlan;
        item.RiskUnitConfirm = existing.RiskUnitConfirm;
        item.RiskRemark = existing.RiskRemark;

        _db.Entry(existing).CurrentValues.SetValues(item);
        existing.Categories = await _db.Categories.Where(c => categoryIds.Contains(c.Id)).ToListAsync();
        existing.Purposes = await _db.Purposes.Where(p => purposeIds.Contains(p.Id)).ToListAsync();
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新盤點項目「{item.SeqNo} {item.DocumentName}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item is not null)
        {
            _db.InventoryItems.Remove(item);
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
        ViewBag.SelectedCategoryIds = selectedCategoryIds ?? Array.Empty<int>();
        ViewBag.SelectedPurposeIds = selectedPurposeIds ?? Array.Empty<int>();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>個人資料風險自評表：以 InventoryItem 的風險欄位維護每筆個資文件的風險評估。</summary>
public class RiskController : Controller
{
    private readonly AppDbContext _db;
    public RiskController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.InventoryItems.AsQueryable();

        q = this.ResolveSearch(q);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(i => i.DocumentName.Contains(q)
                                  || i.SystemCode.Contains(q)
                                  || i.SystemName.Contains(q));

        ViewBag.Query = q;
        return View(await query.OrderBy(i => i.SystemCode).ThenBy(i => i.SeqNo).ToListAsync());
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

        var rows = await query.OrderBy(i => i.SystemCode).ThenBy(i => i.SeqNo).ToListAsync();
        var (content, fileName) = await ExcelExporter.BuildAsync(_db, "Risk", rows);
        return File(content, ExcelExporter.ContentType, fileName);
    }

    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View("Form", new InventoryItem());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryItem model)
    {
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View("Form", model);
        }

        model.RiskValue = CalculateRiskValue(
            model.RiskImpactLevel, model.RiskLikelihoodLevel, model.RiskEffectivenessLevel);
        _db.InventoryItems.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增風險自評「{model.SystemCode} {model.DocumentName}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item is null) return NotFound();
        // 記住這筆的資產編號，回到清單頁時自動帶入搜尋欄
        this.RememberSearch(item.SystemCode);
        return View(item);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item is null) return NotFound();
        // 記住這筆的資產編號，回到清單頁時自動帶入搜尋欄
        this.RememberSearch(item.SystemCode);
        await LoadLookupsAsync();
        return View("Form", item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InventoryItem model)
    {
        if (id != model.Id) return BadRequest();

        var existing = await _db.InventoryItems.FindAsync(id);
        if (existing is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View("Form", model);
        }

        existing.RiskDataSeqNo = model.RiskDataSeqNo;
        existing.RiskCategoryCode = model.RiskCategoryCode;
        existing.RiskCategoryName = model.RiskCategoryName;
        existing.RiskEvent = model.RiskEvent;
        existing.RiskImpactLevel = model.RiskImpactLevel;
        existing.RiskLikelihoodLevel = model.RiskLikelihoodLevel;
        existing.RiskRelatedRegulation = model.RiskRelatedRegulation;
        existing.RiskControlDescription = model.RiskControlDescription;
        existing.RiskEffectivenessLevel = model.RiskEffectivenessLevel;
        existing.RiskImprovementPlan = model.RiskImprovementPlan;
        existing.RiskUnitConfirm = model.RiskUnitConfirm;
        existing.RiskRemark = model.RiskRemark;
        existing.RiskValue = CalculateRiskValue(
            model.RiskImpactLevel, model.RiskLikelihoodLevel, model.RiskEffectivenessLevel);

        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新風險自評「{existing.SystemCode} {existing.DocumentName}」";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>清除該筆盤點項目的風險自評資料（不刪除盤點項目本身）。</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.InventoryItems.FindAsync(id);
        if (existing is not null)
        {
            existing.RiskDataSeqNo = "";
            existing.RiskCategoryCode = "";
            existing.RiskCategoryName = "";
            existing.RiskEvent = "";
            existing.RiskImpactLevel = "";
            existing.RiskLikelihoodLevel = "";
            existing.RiskRelatedRegulation = "";
            existing.RiskControlDescription = "";
            existing.RiskEffectivenessLevel = "";
            existing.RiskValue = "";
            existing.RiskImprovementPlan = "";
            existing.RiskUnitConfirm = "";
            existing.RiskRemark = "";
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除風險自評資料「{existing.SystemCode} {existing.DocumentName}」";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>風險值 = 影響程度 × 發生可能性 × 控制有效性（取各等級前置數字相乘）。</summary>
    private static string CalculateRiskValue(string impact, string likelihood, string effectiveness)
    {
        var i = LeadingLevel(impact);
        var l = LeadingLevel(likelihood);
        var e = LeadingLevel(effectiveness);
        if (i == 0 || l == 0 || e == 0) return "";
        return (i * l * e).ToString();
    }

    private static int LeadingLevel(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        return int.TryParse(value.TrimStart()[..1], out var n) ? n : 0;
    }

    /// <summary>下拉選項改由 3-1~3-4 維護檔（資料庫）帶入。</summary>
    private async Task LoadLookupsAsync()
    {
        ViewBag.RiskCategoryOptions = await _db.RiskCategories
            .OrderBy(r => r.Code)
            .Select(r => new { r.Code, r.CategoryName })
            .ToListAsync();
        ViewBag.CategoryNames = await _db.RiskCategories
            .Select(r => r.CategoryName).Distinct()
            .OrderBy(n => n).ToListAsync();
        ViewBag.ImpactLevels = await _db.RiskImpactLevels
            .OrderBy(r => r.Level).Select(r => r.Level + "：" + r.Name).ToListAsync();
        ViewBag.LikelihoodLevels = await _db.RiskLikelihoodLevels
            .OrderBy(r => r.Level).Select(r => r.Level + "：" + r.Name).ToListAsync();
        ViewBag.EffectivenessLevels = await _db.RiskEffectivenessLevels
            .OrderBy(r => r.Level).Select(r => r.Level + "：" + r.Name).ToListAsync();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>個人資料風險自評表：以 InventoryItem 的風險欄位維護每筆個資文件的風險評估。</summary>
[Authorize(Policy = Policies.ViewAssets)]
public class RiskController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAssetAccess _access;

    public RiskController(AppDbContext db, IAssetAccess access)
    {
        _db = db;
        _access = access;
    }

    /// <param name="team">只看某個組別（SW-權責單位）的資產。與 q 共用同一顆[搜尋]與[清除]。</param>
    public async Task<IActionResult> Index(string? q, string? team)
    {
        q = this.ResolveSearch(q);
        team = this.ResolveTeam(team);

        ViewBag.Query = q;
        ViewBag.Team = team;
        return View(await FilteredAsync(q, team));
    }

    /// <summary>清單與匯出共用的篩選，兩邊各寫一套遲早會對不上。</summary>
    private async Task<List<InventoryItem>> FilteredAsync(string? q, string? team)
    {
        // 匯出要帶出「使用資料(欄位)」與「特定目的」這兩個多對多欄位
        var query = _db.InventoryItems
            .Include(i => i.Categories)
            .Include(i => i.Purposes)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(i => i.DocumentName.Contains(q)
                                  || i.SystemCode.Contains(q)
                                  || i.SystemName.Contains(q));

        var codes = await TeamFilter.CodesOfAsync(_db, team);
        if (codes is not null)
            query = query.Where(i => codes.Contains(i.SystemCode));

        return await query.OrderBy(i => i.SystemCode).ThenBy(i => i.SeqNo).ToListAsync();
    }

    /// <summary>匯出目前搜尋結果。q 由畫面帶入，與清單所見一致，不更動搜尋記憶。</summary>
    public async Task<IActionResult> Export(string? q, string? team)
    {
        var rows = await FilteredAsync(q, team);
        var (content, fileName) = await ExcelExporter.BuildAsync(_db, "Risk", rows);
        return File(content, ExcelExporter.ContentType, fileName);
    }

    [Authorize(Policy = Policies.CreateAssets)]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View("Form", new InventoryItem());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CreateAssets)]
    public async Task<IActionResult> Create(InventoryItem model)
    {
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View("Form", model);
        }

        // 名稱一律依編號查出，不採信畫面送回來的值
        model.SystemName = await AssetPicker.ResolveNameAsync(_db, model.SystemCode) ?? model.SystemName;

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

        // 只能異動自己科別負責的資產底下的資料；直接輸入網址也必須擋下
        if (!await _access.CanModifyAsync(item.SystemCode)) return Forbid();

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

        // 原資產與改後的資產都必須在權限範圍內，否則使用者可以把不屬於自己科別的資料
        // 改成自己的資產編號、或把自己的資料丟給別人
        if (!await _access.CanModifyAsync(existing.SystemCode)
            || !await _access.CanModifyAsync(model.SystemCode)) return Forbid();

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View("Form", model);
        }

        // 資產編號可在本畫面調整（編號 19～21 那幾筆只存在於風險自評，得從這裡維護），
        // 名稱依編號查出，不採信畫面送回來的值
        existing.SystemCode = model.SystemCode;
        existing.SystemName = await AssetPicker.ResolveNameAsync(_db, model.SystemCode) ?? existing.SystemName;

        // 個資文件/檔案名稱同樣可以在這裡改。它是 InventoryItems 的欄位，個資盤點表
        // 也看得到同一格——兩張清單本來就是同一批資料的兩個面，改這裡那邊會跟著變。
        existing.DocumentName = model.DocumentName;

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
            // 能改就能刪（業務端 0918）。
            // ⚠ 這裡不是軟刪除：清空的是這一筆的風險自評欄位，清掉就救不回來
            //   （個資盤點項目本身與風險自評共用同一列，不能對整列加刪除註記）
            if (!await _access.CanDeleteAsync(existing.SystemCode)) return Forbid();

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
        ViewBag.AssetOptions = await AssetPicker.LoadAsync(_db);
    }
}

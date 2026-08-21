using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Models.ViewModels;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>Sheet3：資訊系統、資料庫與檔案伺服器盤點表</summary>
[Authorize(Policy = Policies.ViewAssets)]
public class SystemsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAssetAccess _access;

    public SystemsController(AppDbContext db, ICurrentUser currentUser, IAssetAccess access)
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

        ViewBag.Query = q;
        return View(await query.OrderBy(s => s.SeqNo).ToListAsync());
    }

    /// <summary>匯出目前搜尋結果。q 由畫面帶入，與清單所見一致，不更動搜尋記憶。</summary>
    public async Task<IActionResult> Export(string? q)
    {
        var query = _db.InfoSystems.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.SystemCode.Contains(q)
                                  || s.SystemName.Contains(q));

        var rows = await query.OrderBy(s => s.SeqNo).ToListAsync();
        var (content, fileName) = await ExcelExporter.BuildAsync(_db, "Systems", rows);
        return File(content, ExcelExporter.ContentType, fileName);
    }

    /// <param name="from">從哪張清單按的[新增]，決定[取消]與新增後回到哪裡。</param>
    [Authorize(Policy = Policies.ManageAssets)]
    public IActionResult Create(string? from)
    {
        ViewBag.From = ListSource.Resolve(from);
        return View("CreateAll", new InfoSystemEditViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageAssets)]
    public async Task<IActionResult> Create(InfoSystemEditViewModel model, string? from)
    {
        var source = ListSource.Resolve(from);
        ViewBag.From = source;

        // 統一新增畫面一次送出 SW / DA / 系統盤點三個區塊，整筆一起建立
        var system = new InfoSystem();
        InfoSystemBlocks.CopyToEntity(system, model.Software, InfoSystemBlocks.Sw);
        InfoSystemBlocks.CopyToEntity(system, model.Data, InfoSystemBlocks.Da);
        InfoSystemBlocks.CopyToEntity(system, model.Sheet3, InfoSystemBlocks.Sheet3);

        await ValidateUniqueKeysAsync(system);
        if (!ModelState.IsValid) return View("CreateAll", model);

        _db.InfoSystems.Add(system);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增資訊資產「{system.SystemCode} {system.SystemName}」";
        return RedirectToAction("Index", source);
    }

    /// <param name="from">來源清單頁，供[取消]回到原處。</param>
    public async Task<IActionResult> Edit(int id, string? from)
    {
        var system = await _db.InfoSystems.FindAsync(id);
        if (system is null) return NotFound();

        // 資產負責人只能異動名下的資產；清單頁雖然不會顯示按鈕，直接輸入網址仍必須擋下
        if (!await _access.CanModifyAsync(system.SystemCode)) return Forbid();

        // 記住這筆的資產編號，回到清單頁時自動帶入搜尋欄
        this.RememberSearch(system.SystemCode);
        ViewBag.From = ListSource.Resolve(from);
        return View("EditAll", InfoSystemBlocks.ToEditViewModel(system));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Sheet3")] SystemEditViewModel model, string? from)
    {
        if (id != model.Id) return BadRequest();
        ViewBag.From = ListSource.Resolve(from);

        var existing = await _db.InfoSystems.FindAsync(id);
        if (existing is null) return NotFound();

        // 資產負責人只能異動名下的資產；清單頁雖然不會顯示按鈕，直接輸入網址仍必須擋下
        if (!await _access.CanModifyAsync(existing.SystemCode)) return Forbid();

        // 唯一性要用「改後的值」判斷，先套到一份暫時的實體上再檢查
        var candidate = new InfoSystem { Id = existing.Id };
        InfoSystemBlocks.CopyToEntity(candidate, model, InfoSystemBlocks.Sheet3);
        await ValidateUniqueKeysAsync(candidate);

        if (!ModelState.IsValid)
        {
            var reload = InfoSystemBlocks.ToEditViewModel(existing);
            reload.Sheet3 = model;
            return View("EditAll", reload);
        }

        InfoSystemBlocks.CopyToEntity(existing, model, InfoSystemBlocks.Sheet3);
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
        TempData["Message"] = $"已更新系統「{existing.SeqNo} {existing.SystemName}」";
        // 統一編輯畫面：存完留在原畫面，方便接著編其他區塊（from 要一起帶著，[取消]才知道回哪）
        return RedirectToAction("Edit", "Systems", new { id, from });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var system = await _db.InfoSystems.FindAsync(id);
        if (system is not null)
        {
            // 資產負責人只能異動名下的資產；清單頁雖然不會顯示按鈕，直接輸入網址仍必須擋下
            if (!await _access.CanModifyAsync(system.SystemCode)) return Forbid();
            // 軟刪除：只加註記，資料仍留在資料庫，但被全域查詢篩選排除，
            // 因此 SW／DA／系統盤點三張清單與匯出都不會再出現。
            system.IsDeleted = true;
            system.DeletedAt = DateTime.Now;
            system.DeletedBy = _currentUser.Name;
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除系統「{system.SeqNo} {system.SystemName}」";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>系統盤點區塊在表單中的欄位前綴，驗證訊息要用同一個名稱才對得上輸入框。</summary>
    private const string Sheet3Prefix = nameof(InfoSystemEditViewModel.Sheet3);

    /// <summary>資產編號與編號在主檔須唯一（資料庫也有對應的唯一索引）。</summary>
    private async Task ValidateUniqueKeysAsync(InfoSystem system)
    {
        if (!string.IsNullOrWhiteSpace(system.SystemCode)
            && await _db.InfoSystems.AnyAsync(s => s.SystemCode == system.SystemCode && s.Id != system.Id))
            ModelState.AddModelError($"{Sheet3Prefix}.{nameof(InfoSystem.SystemCode)}",
                                     $"資產編號 {system.SystemCode} 已存在");

        if (!string.IsNullOrWhiteSpace(system.SeqNo)
            && await _db.InfoSystems.AnyAsync(s => s.SeqNo == system.SeqNo && s.Id != system.Id))
            ModelState.AddModelError($"{Sheet3Prefix}.{nameof(InfoSystem.SeqNo)}",
                                     $"編號 {system.SeqNo} 已存在");
    }
}

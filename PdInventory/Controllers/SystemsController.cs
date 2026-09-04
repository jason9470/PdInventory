using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Models.ViewModels;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>
/// 資訊系統、資料庫與檔案伺服器盤點表。
///
/// 盤點表已獨立成 SystemInventories 資料表，但依業務端確認一定依附於某個軟體資產，
/// 因此資產編號必填。本控制器同時提供 SW／DA／盤點表的統一新增與編輯畫面。
/// </summary>
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
        var (content, fileName) = await ExcelExporter.BuildAsync(_db, "Systems", rows);
        return File(content, ExcelExporter.ContentType, fileName);
    }

    private async Task<List<SystemInventory>> FilteredAsync(string? q)
    {
        var query = _db.SystemInventories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(i => i.SystemCode.Contains(q) || i.DbName.Contains(q));

        return await query.OrderBy(i => i.SystemCode).ToListAsync();
    }

    /// <summary>編號與資產名稱屬於軟體資產那一邊，清單要顯示就得補查。</summary>
    private async Task LoadAssetLookupAsync(List<SystemInventory> rows)
    {
        var codes = rows.Select(i => i.SystemCode).Distinct().ToList();
        ViewBag.Assets = await _db.InfoSystems
            .Where(s => codes.Contains(s.SystemCode))
            .ToDictionaryAsync(s => s.SystemCode);
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

        // 統一新增畫面一次送出三個區塊，但它們現在分屬三張表，整批一起建立
        var system = new InfoSystem();
        InfoSystemBlocks.Copy(system, model.Software);

        await ValidateUniqueKeysAsync(system);
        if (!ModelState.IsValid) return View("CreateAll", model);

        _db.InfoSystems.Add(system);

        var dataAsset = new DataAsset();
        InfoSystemBlocks.Copy(dataAsset, model.Data);
        dataAsset.SystemCode = system.SystemCode;
        if (HasContent(dataAsset, nameof(DataAsset.SystemCode))) _db.DataAssets.Add(dataAsset);

        var inventory = new SystemInventory();
        InfoSystemBlocks.Copy(inventory, model.Sheet3);
        inventory.SystemCode = system.SystemCode;
        if (HasContent(inventory, nameof(SystemInventory.SystemCode))) _db.SystemInventories.Add(inventory);

        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增資訊資產「{system.SystemCode} {system.SystemName}」";
        return RedirectToAction("Index", source);
    }

    /// <summary>
    /// 三個區塊都沒填就不要建出空的資料列。資產編號是從 SW 帶過去的，
    /// 判斷「有沒有內容」時要排除它，否則永遠都算有值。
    /// </summary>
    private static bool HasContent(object entity, string ignoreProperty) =>
        entity.GetType()
            .GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.Name != ignoreProperty
                        && p.Name is not ("CreatedBy" or "UpdatedBy"))
            .Any(p => !string.IsNullOrWhiteSpace(p.GetValue(entity) as string));

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
        return View("EditAll", await BuildEditModelAsync(system));
    }

    /// <param name="id">統一編輯畫面所在的 InfoSystems 主鍵；SystemInventories 的主鍵在 model.Id。</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Sheet3")] SystemEditViewModel model, string? from)
    {
        ViewBag.From = ListSource.Resolve(from);

        var system = await _db.InfoSystems.FindAsync(id);
        if (system is null) return NotFound();

        if (!await _access.CanModifyAsync(system.SystemCode)) return Forbid();

        if (!ModelState.IsValid)
        {
            var reload = await BuildEditModelAsync(system);
            reload.Sheet3 = model;
            return View("EditAll", reload);
        }

        var existing = model.Id > 0 ? await _db.SystemInventories.FindAsync(model.Id) : null;
        if (existing is null)
        {
            existing = new SystemInventory();
            _db.SystemInventories.Add(existing);
        }
        else
        {
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

        TempData["Message"] = $"已更新系統盤點「{system.SystemCode} {system.SystemName}」";
        return RedirectToAction("Edit", "Systems", new { id, from });
    }

    /// <param name="id">SystemInventories 的主鍵。</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var inventory = await _db.SystemInventories.FindAsync(id);
        if (inventory is not null)
        {
            if (!await _access.CanModifyAsync(inventory.SystemCode)) return Forbid();

            // 軟刪除：只加註記，全域查詢篩選讓它從清單、檢視與匯出中消失
            inventory.IsDeleted = true;
            inventory.DeletedAt = DateTime.Now;
            inventory.DeletedBy = _currentUser.Name;
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已刪除系統盤點「{inventory.SystemCode}」";
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

    /// <summary>系統盤點區塊在表單中的欄位前綴，驗證訊息要用同一個名稱才對得上輸入框。</summary>
    private const string SoftwarePrefix = nameof(InfoSystemEditViewModel.Software);

    /// <summary>資產編號與編號在主檔須唯一（資料庫也有對應的唯一索引）。</summary>
    private async Task ValidateUniqueKeysAsync(InfoSystem system)
    {
        if (!string.IsNullOrWhiteSpace(system.SystemCode)
            && await _db.InfoSystems.AnyAsync(s => s.SystemCode == system.SystemCode && s.Id != system.Id))
            ModelState.AddModelError($"{SoftwarePrefix}.{nameof(InfoSystem.SystemCode)}",
                                     $"資產編號 {system.SystemCode} 已存在");

        if (!string.IsNullOrWhiteSpace(system.SeqNo)
            && await _db.InfoSystems.AnyAsync(s => s.SeqNo == system.SeqNo && s.Id != system.Id))
            ModelState.AddModelError($"{SoftwarePrefix}.{nameof(InfoSystem.SeqNo)}",
                                     $"編號 {system.SeqNo} 已存在");
    }
}

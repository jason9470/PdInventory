using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Models.ViewModels;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>
/// 資訊資產清單－資料(DA)，同時是 SW／DA／盤點表統一新增、編輯與檢視畫面的入口。
///
/// 為什麼主鍵在這裡而不在 SW：有 DA 不一定有 SW（各組的 PC／NB 那類資料資產就沒有），
/// 以 SW 當主鍵時那些資料根本沒有網址可以開。反過來每個 SW 都配一列 DA，
/// 所以 DA 是唯一涵蓋得了全部資料的入口。三個面怎麼湊起來見 <see cref="AssetGroup"/>。
/// </summary>
[Authorize(Policy = Policies.ViewAssets)]
public class DataController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAssetAccess _access;

    public DataController(AppDbContext db, ICurrentUser currentUser, IAssetAccess access)
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
        var (content, fileName) = await ExcelExporter.BuildAsync(_db, "Data", rows);
        return File(content, ExcelExporter.ContentType, fileName);
    }

    /// <summary>
    /// 資料資產編號空白的不列出來。那是 0910 為了「每個 SW 都配一列 DA」補建的空白列
    /// ——它們只是讓統一畫面有主鍵可用，本身沒有任何資料資產的內容，
    /// 出現在清單與匯出檔裡只會是幾行空白。要編它們請從 SW 清單或盤點表清單進去。
    /// </summary>
    private async Task<List<DataAsset>> FilteredAsync(string? q)
    {
        var query = _db.DataAssets.Where(d => d.DaAssetCode.Trim() != "");

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(d => d.DaAssetCode.Contains(q) || d.SystemCode.Contains(q));

        return await query.OrderBy(d => d.DaAssetCode).ToListAsync();
    }

    /// <summary>
    /// 資產狀態與資產名稱屬於軟體資產那一邊，清單要顯示就得補查。
    /// 沒有關連 SW 的資料資產（例如各組的 PC／NB）查不到，畫面留空即可。
    /// </summary>
    private async Task LoadAssetLookupAsync(List<DataAsset> rows)
    {
        var codes = rows.Select(d => d.SystemCode)
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Distinct()
                        .ToList();

        ViewBag.Assets = await _db.InfoSystems
            .Where(s => codes.Contains(s.SystemCode))
            .ToDictionaryAsync(s => s.SystemCode);
    }

    // ── 統一新增畫面 ────────────────────────────────────────────────

    /// <param name="from">從哪張清單按的[新增]，決定[取消]與新增後回到哪裡。</param>
    [Authorize(Policy = Policies.ManageAssets)]
    public IActionResult Create(string? from)
    {
        ViewBag.From = ListSource.Resolve(from);
        return View("CreateAll", new InfoSystemEditViewModel());
    }

    /// <summary>
    /// 三個區塊一次送出。分成兩條路：
    ///
    /// 勾了「無SW資產編號」就只建 DataAssets 一列，SW 與盤點表完全不碰——那兩個區塊在
    /// 畫面上已經隱藏且欄位被停用（停用的欄位瀏覽器不會送出），但那只是操作防呆，
    /// 改個表單就繞過去了，所以這裡把送進來的內容整個丟掉、驗證錯誤也一併清掉
    /// （SW 的資產名稱是必填，不清掉會擋住存檔）。
    ///
    /// 沒勾就是完整的一套系統：SW 與 DA 一定成對建立（DA 是統一畫面的主鍵，缺了它
    /// 這筆 SW 之後就打不開），盤點表則是有填才建——55 套系統裡本來就只有 26 套有。
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageAssets)]
    public async Task<IActionResult> Create(InfoSystemEditViewModel model, string? from)
    {
        var source = ListSource.Resolve(from);
        ViewBag.From = source;

        if (model.NoSoftwareAsset)
        {
            model.Software = new SoftwareEditViewModel();
            model.Sheet3 = new SystemEditViewModel();
            foreach (var key in ModelState.Keys
                         .Where(k => k.StartsWith($"{nameof(InfoSystemEditViewModel.Software)}.")
                                  || k.StartsWith($"{nameof(InfoSystemEditViewModel.Sheet3)}."))
                         .ToList())
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid) return View("CreateAll", model);

            var orphan = new DataAsset();
            InfoSystemBlocks.Copy(orphan, model.Data);
            orphan.SystemCode = "";
            _db.DataAssets.Add(orphan);

            await _db.SaveChangesAsync();
            TempData["Message"] = $"已新增資料資產「{orphan.DaAssetCode}」（無關連軟體資產）";
            return RedirectToAction(nameof(Index));
        }

        var system = new InfoSystem();
        InfoSystemBlocks.Copy(system, model.Software);

        await ValidateUniqueKeysAsync(system);
        if (!ModelState.IsValid) return View("CreateAll", model);

        _db.InfoSystems.Add(system);

        // 一定要建：DA 的主鍵就是統一編輯畫面的網址，沒有它這筆 SW 之後開不起來
        var dataAsset = new DataAsset();
        InfoSystemBlocks.Copy(dataAsset, model.Data);
        dataAsset.SystemCode = system.SystemCode;
        _db.DataAssets.Add(dataAsset);

        var inventory = new SystemInventory();
        InfoSystemBlocks.Copy(inventory, model.Sheet3);
        inventory.SystemCode = system.SystemCode;
        if (HasContent(inventory, nameof(SystemInventory.SystemCode))) _db.SystemInventories.Add(inventory);

        await _db.SaveChangesAsync();
        TempData["Message"] = $"已新增資訊資產「{system.SystemCode} {system.SystemName}」";
        return RedirectToAction("Index", source);
    }

    /// <summary>
    /// 區塊都沒填就不要建出空的資料列。資產編號是從 SW 帶過去的，
    /// 判斷「有沒有內容」時要排除它，否則永遠都算有值。
    /// </summary>
    private static bool HasContent(object entity, string ignoreProperty) =>
        entity.GetType()
            .GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.Name != ignoreProperty
                        && p.Name is not ("CreatedBy" or "UpdatedBy"))
            .Any(p => !string.IsNullOrWhiteSpace(p.GetValue(entity) as string));

    /// <summary>資產編號與編號在主檔須唯一（資料庫也有對應的唯一索引）。</summary>
    private async Task ValidateUniqueKeysAsync(InfoSystem system)
    {
        const string prefix = nameof(InfoSystemEditViewModel.Software);

        if (!string.IsNullOrWhiteSpace(system.SystemCode)
            && await _db.InfoSystems.AnyAsync(s => s.SystemCode == system.SystemCode && s.Id != system.Id))
            ModelState.AddModelError($"{prefix}.{nameof(InfoSystem.SystemCode)}",
                                     $"資產編號 {system.SystemCode} 已存在");

        if (!string.IsNullOrWhiteSpace(system.SeqNo)
            && await _db.InfoSystems.AnyAsync(s => s.SeqNo == system.SeqNo && s.Id != system.Id))
            ModelState.AddModelError($"{prefix}.{nameof(InfoSystem.SeqNo)}",
                                     $"編號 {system.SeqNo} 已存在");
    }

    // ── 統一編輯與檢視畫面 ──────────────────────────────────────────

    /// <param name="id">DataAssets 的主鍵。</param>
    /// <param name="from">來源清單頁，供[取消]回到原處。</param>
    public async Task<IActionResult> Edit(int id, string? from)
    {
        var group = await AssetGroups.LoadAsync(_db, id);
        if (group is null) return NotFound();

        // 資產負責人只能異動名下的資產；清單頁雖然不會顯示按鈕，直接輸入網址仍必須擋下。
        // 沒有關連 SW 的資料資產無人可認領，CanModifyAsync 對空白編號一律回 false，
        // 因此那些資料只有主管以上進得來——這是業務端 0910 確認的規則。
        if (!await _access.CanModifyAsync(group.SystemCode)) return Forbid();

        // 記住這筆的資產編號，回到清單頁時自動帶入搜尋欄
        this.RememberSearch(group.SystemCode);
        ViewBag.From = ListSource.Resolve(from);
        return View("EditAll", InfoSystemBlocks.ToEditViewModel(group));
    }

    /// <param name="id">DataAssets 的主鍵，也是統一編輯畫面的網址參數。</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Data")] DataEditViewModel model, string? from)
    {
        ViewBag.From = ListSource.Resolve(from);

        var group = await AssetGroups.LoadAsync(_db, id);
        if (group is null) return NotFound();

        if (!await _access.CanModifyAsync(group.SystemCode)) return Forbid();

        if (!ModelState.IsValid)
        {
            var reload = InfoSystemBlocks.ToEditViewModel(group);
            reload.Data = model;
            return View("EditAll", reload);
        }

        var existing = group.Data;

        // 與 SW 的關聯一旦建立就不再改動（業務端 0910 確認）：資產編號串起六張表、
        // 又是權限判斷的依據，在這裡被改掉會讓那些關聯瞬間斷開。
        // DA 區塊沒有這個欄位，送來的是空字串，因此複製前先把現值留下來。
        var systemCode = group.SystemCode;
        InfoSystemBlocks.Copy(existing, model);
        existing.SystemCode = systemCode;

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
            return RedirectToAction(nameof(Edit), new { id, from });
        }

        TempData["Message"] = $"已更新資料資產「{existing.DaAssetCode}」";
        // 統一編輯畫面：存完留在原畫面，方便接著編其他區塊（from 要一起帶著，[取消]才知道回哪）
        return RedirectToAction(nameof(Edit), new { id, from });
    }

    /// <summary>SW／DA／系統盤點三張清單共用的檢視畫面。</summary>
    /// <param name="id">DataAssets 的主鍵。</param>
    /// <param name="from">來源清單頁，供[回列表]回到原處。</param>
    public async Task<IActionResult> Details(int id, string? from)
    {
        var group = await AssetGroups.LoadAsync(_db, id);
        if (group is null) return NotFound();

        // 記住這筆的資產編號，之後進到任一清單頁都會自動帶入搜尋欄
        this.RememberSearch(group.SystemCode);
        ViewBag.From = ListSource.Resolve(from);

        // 畫面綁的是 InfoSystem（欄位標題都靠它的 [Display] 取），沒有關連 SW 時
        // 餵一個空實體並把 HasSoftware 關掉，那兩個區塊整塊不顯示。
        ViewBag.HasSoftware = group.HasSoftware;
        ViewBag.DataAssetId = group.Data.Id;
        ViewBag.DataAsset = group.Data;
        ViewBag.Inventory = group.Inventory;
        return View(group.System ?? new InfoSystem());
    }

    // ── 刪除 ────────────────────────────────────────────────────────

    /// <summary>
    /// 有關連 SW 的，SW／DA／盤點表三列一起軟刪除——三張清單是同一筆資產的三個面，
    /// 在這裡刪掉它就是整筆不要了（業務端 0910 確認）。只刪 DA 也不可行：
    /// 那樣 SW 清單那一列就再也沒有編輯入口了。
    ///
    /// 沒有關連 SW 的（各組的 PC／NB）本來就只有這一列，刪它就是刪它。
    /// </summary>
    /// <param name="id">DataAssets 的主鍵。</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var group = await AssetGroups.LoadAsync(_db, id);
        if (group is null) return RedirectToAction(nameof(Index));

        // 沒有關連 SW 的資料資產無人可認領，只有主管以上能刪
        if (!await _access.CanModifyAsync(group.SystemCode)) return Forbid();

        AssetGroups.SoftDelete(group, _currentUser.Name);
        await _db.SaveChangesAsync();

        TempData["Message"] = group.HasSoftware
            ? $"已刪除資訊資產「{group.SystemCode} {group.System!.SystemName}」的 SW、DA 與盤點表資料"
            : $"已刪除資料資產「{group.Data.DaAssetCode}」";
        return RedirectToAction(nameof(Index));
    }
}

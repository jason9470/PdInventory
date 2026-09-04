using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>
/// 欄位選項的維護畫面。六（以後會更多）個欄位共用這一個控制器，
/// 靠 <c>?field=</c> 決定維護的是哪一個——作法比照「維護匯出」的 <c>?list=</c>，
/// 否則每個欄位都要複製一份幾乎一樣的 CRUD。
///
/// 哪些欄位走這條路見 <see cref="OptionCatalog"/>。
/// </summary>
[Authorize(Policy = Policies.Admin)]
public class FieldOptionItemsController : Controller
{
    private readonly AppDbContext _db;
    public FieldOptionItemsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? field)
    {
        var target = OptionCatalog.Find(field);
        if (target is null) return NotFound();

        ViewBag.Field = target;
        ViewBag.UsageCounts = await CountUsagesAsync(target);
        return View(await ItemsOfAsync(target.Field));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string field, string value, string? remark)
    {
        var target = OptionCatalog.Find(field);
        if (target is null) return NotFound();

        value = (value ?? "").Trim();
        if (value.Length == 0)
            TempData["Error"] = "選項不可空白。";
        else if (await _db.FieldOptionItems.AnyAsync(o => o.FieldName == field && o.Value == value))
            TempData["Error"] = $"選項「{value}」已存在。";
        else if (target.IsMultiple && value.Contains(MultiValue.Separator))
            // 多選欄位以 / 分隔，選項本身含斜線的話存進去就切成兩半了
            TempData["Error"] = $"多選欄位的選項不能包含「{MultiValue.Separator}」，那是分隔符號。";
        else
        {
            var order = await _db.FieldOptionItems.Where(o => o.FieldName == field)
                                                  .MaxAsync(o => (int?)o.SortOrder) ?? 0;
            _db.FieldOptionItems.Add(new FieldOptionItem
            {
                FieldName = field,
                Value = value,
                SortOrder = order + 1,
                Remark = remark?.Trim() ?? "",
            });
            await _db.SaveChangesAsync();
            TempData["Message"] = $"已新增選項「{value}」。";
        }

        return RedirectToAction(nameof(Index), new { field });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string? remark)
    {
        var item = await _db.FieldOptionItems.FindAsync(id);
        if (item is null) return NotFound();

        // 只能改備註與順序，不能改值：值就是各表單欄位裡存的文字，
        // 在這裡改掉不會連帶更新那些資料，只會讓它們變成選不到的舊值。
        item.Remark = remark?.Trim() ?? "";
        await _db.SaveChangesAsync();

        TempData["Message"] = $"已更新選項「{item.Value}」的備註。";
        return RedirectToAction(nameof(Index), new { field = item.FieldName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int delta)
    {
        var item = await _db.FieldOptionItems.FindAsync(id);
        if (item is null) return NotFound();

        var items = await ItemsOfAsync(item.FieldName);
        var index = items.FindIndex(o => o.Id == id);
        var swapWith = index + delta;

        if (swapWith >= 0 && swapWith < items.Count)
        {
            (items[index].SortOrder, items[swapWith].SortOrder) =
                (items[swapWith].SortOrder, items[index].SortOrder);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), new { field = item.FieldName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.FieldOptionItems.FindAsync(id);
        if (item is null) return RedirectToAction(nameof(Index));

        var target = OptionCatalog.Find(item.FieldName);
        var used = target is null ? 0 : await CountUsageAsync(target, item.Value);
        if (used > 0)
        {
            // 與部門、人員的維護畫面同一套規則：還被引用就不給刪，
            // 否則那些資料會變成選不到的孤兒值
            TempData["Error"] = $"「{item.Value}」還被 {used} 筆資料使用中，請先改掉那些資料再刪除。";
            return RedirectToAction(nameof(Index), new { field = item.FieldName });
        }

        _db.FieldOptionItems.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已刪除選項「{item.Value}」。";
        return RedirectToAction(nameof(Index), new { field = item.FieldName });
    }

    private Task<List<FieldOptionItem>> ItemsOfAsync(string field) => _db.FieldOptionItems
        .Where(o => o.FieldName == field)
        .OrderBy(o => o.SortOrder).ThenBy(o => o.Id)
        .ToListAsync();

    /// <summary>每個選項各被幾筆資料使用，供畫面顯示與刪除前的檢查。</summary>
    private async Task<Dictionary<string, int>> CountUsagesAsync(OptionField target)
    {
        var stored = await StoredValuesAsync(target);
        var items = await ItemsOfAsync(target.Field);

        return items.ToDictionary(
            o => o.Value,
            o => stored.Count(v => MultiValue.Contains(v, o.Value)));
    }

    private async Task<int> CountUsageAsync(OptionField target, string value)
    {
        var stored = await StoredValuesAsync(target);
        return stored.Count(v => MultiValue.Contains(v, value));
    }

    /// <summary>
    /// 這個欄位目前存了哪些值。屬性名稱是動態的，用 EF.Property 取，
    /// 這樣 OptionCatalog 多登記一個同表的欄位時這裡不必跟著改。
    ///
    /// 以屬性掛在哪個實體上決定要查哪張表。若日後有欄位登記在這三張以外的實體，
    /// 這裡會擲出例外而不是安靜地算成 0——寧可當場壞掉，也不要讓刪除保護失效。
    /// </summary>
    private async Task<List<string>> StoredValuesAsync(OptionField target)
    {
        if (typeof(InfoSystem).GetProperty(target.Field) is not null)
            return await _db.InfoSystems.Select(s => EF.Property<string>(s, target.Field)).ToListAsync();

        if (typeof(DataAsset).GetProperty(target.Field) is not null)
            return await _db.DataAssets.Select(d => EF.Property<string>(d, target.Field)).ToListAsync();

        if (typeof(InventoryItem).GetProperty(target.Field) is not null)
            return await _db.InventoryItems.Select(i => EF.Property<string>(i, target.Field)).ToListAsync();

        throw new InvalidOperationException(
            $"OptionCatalog 登記的欄位 {target.Field} 不屬於 InfoSystem／DataAsset／InventoryItem，"
            + "請在 StoredValuesAsync 補上對應的資料表。");
    }
}

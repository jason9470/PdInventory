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
// 主管看得到但不能改（0918）：類別層級只要求可檢視，修改動作另外掛 Admin
[Authorize(Policy = Policies.ViewMaintenance)]
public class FieldOptionItemsController : Controller
{
    private readonly AppDbContext _db;
    public FieldOptionItemsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? field)
    {
        var target = OptionCatalog.Find(field);
        if (target is null) return NotFound();

        ViewBag.Field = target;
        ViewBag.UsageCounts = await LookupUsage.CountsAsync(_db, UsageKind.FieldOption, target.Field);
        return View(await ItemsOfAsync(target.Field));
    }

    [Authorize(Policy = Policies.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string field, string value, string? remark)
    {
        var target = OptionCatalog.Find(field);
        if (target is null) return NotFound();

        value = (value ?? "").Trim();
        var reject = value.Length == 0 ? null : target.RejectReason(value);

        if (value.Length == 0)
            TempData["Error"] = "選項不可空白。";
        else if (await _db.FieldOptionItems.AnyAsync(o => o.FieldName == field && o.Value == value))
            TempData["Error"] = $"選項「{value}」已存在。";
        else if (reject is not null)
            TempData["Error"] = reject;
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

    [Authorize(Policy = Policies.Admin)]
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

    [Authorize(Policy = Policies.Admin)]
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

    [Authorize(Policy = Policies.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.FieldOptionItems.FindAsync(id);
        if (item is null) return RedirectToAction(nameof(Index));

        var target = OptionCatalog.Find(item.FieldName);

        // 程式碼直接依賴的選項不准刪，刪了會讓別處靜靜地失效
        if (target is not null && target.IsReserved(item.Value))
        {
            TempData["Error"] = $"「{item.Value}」是程式邏輯依賴的選項，不能刪除。";
            return RedirectToAction(nameof(Index), new { field = item.FieldName });
        }

        var used = target is null
            ? 0
            : await LookupUsage.CountAsync(_db, UsageKind.FieldOption, item.Value, target.Field);
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

}

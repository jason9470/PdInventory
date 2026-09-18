using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdInventory.Data;
using PdInventory.Helpers;
using PdInventory.Models.ViewModels;

namespace PdInventory.Controllers;

/// <summary>
/// 「使用中」明細。各維護畫面的「N 筆」按下去會開一個彈跳視窗，內容由這裡以部分檢視回傳。
///
/// 為什麼走部分檢視而不是各自一個頁面：維護畫面可能已經搜尋過、捲到一半，
/// 導頁會把那些狀態弄丟。看完關掉就回到原本的位置（作法比照盤點重點那個視窗）。
///
/// 數字與明細都出自 <see cref="LookupUsage.RowsAsync"/>，因此不可能出現
/// 「徽章寫 3 筆、點開只有 2 筆」。
/// </summary>
// 「使用中」明細只是查詢，主管看得到維護畫面就要看得到明細
[Authorize(Policy = Policies.ViewMaintenance)]
public class UsageController : Controller
{
    private readonly AppDbContext _db;
    public UsageController(AppDbContext db) => _db = db;

    /// <param name="kind">哪一種維護資料。</param>
    /// <param name="value">維護表裡的那個值（部門名稱、人名、選項值、代號、等級…）。</param>
    /// <param name="field"><see cref="UsageKind.FieldOption"/> 專用：是哪一個欄位。</param>
    /// <param name="title">視窗標題顯示的名稱；沒給就用 value。</param>
    public async Task<IActionResult> List(UsageKind kind, string value, string? field, string? title)
    {
        if (string.IsNullOrEmpty(value)) return BadRequest();

        var rows = (await LookupUsage.RowsAsync(_db, kind, field)).GetValueOrDefault(value) ?? [];

        return PartialView("_List", new UsageListViewModel(
            string.IsNullOrWhiteSpace(title) ? value : title, rows));
    }
}

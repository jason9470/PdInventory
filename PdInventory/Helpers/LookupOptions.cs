using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;

namespace PdInventory.Helpers;

/// <summary>
/// 來自維護資料表的下拉選項（部門、維運人員…）。
///
/// 與 <see cref="FieldOptions"/> 的分工：那裡放的是本質上不會變、寫死在程式裡就好的
/// （是／否、等級 1~4）；會隨組織異動、要由管理者自行維護的一律走這裡。
///
/// 之所以做成注入的服務、由畫面直接取用，是因為這類欄位會愈來愈多：
/// 每多一個下拉都要在三、四個控制器補 ViewBag，很快就會有人漏掉一個畫面。
/// 註冊為 Scoped，同一個請求內只查一次資料庫。
/// </summary>
public sealed class LookupOptions
{
    private readonly AppDbContext _db;
    private List<string>? _departments;
    private List<string>? _opsStaff;

    public LookupOptions(AppDbContext db) => _db = db;

    /// <summary>部門名稱，代號待補的排在最後（與維護畫面同一個順序）。</summary>
    public IReadOnlyList<string> Departments => _departments ??= _db.Departments
        .OrderBy(d => d.CostCenter == "").ThenBy(d => d.CostCenter).ThenBy(d => d.Name)
        .Select(d => d.Name)
        .ToList();

    /// <summary>維運人員姓名，組別待補的排在最後。</summary>
    public IReadOnlyList<string> OpsStaff => _opsStaff ??= _db.OpsStaffs
        .OrderBy(o => o.TeamName == "").ThenBy(o => o.TeamName).ThenBy(o => o.Name)
        .Select(o => o.Name)
        .ToList();

    /// <summary>
    /// 單選下拉。目前值不在選項中時額外插入一個標示過的選項並選中它——
    /// 否則 &lt;select&gt; 會顯示空白，使用者一按儲存就把原本的資料清掉。
    /// </summary>
    public List<SelectListItem> ItemsFor(IReadOnlyList<string> source, string? currentValue)
    {
        var items = source
            .Select(v => new SelectListItem(v, v, v == currentValue))
            .ToList();

        if (!string.IsNullOrWhiteSpace(currentValue) && !source.Contains(currentValue))
            items.Insert(0, new SelectListItem($"{currentValue}（目前值，不在選項中）", currentValue, true));

        return items;
    }
}

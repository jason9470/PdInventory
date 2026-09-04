using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

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
    private List<string>? _employees;
    private List<string>? _employeeManagers;
    private List<string>? _vendors;
    private readonly Dictionary<string, IReadOnlyList<string>> _fieldOptions = [];

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

    /// <summary>人員姓名（全部），組別待補的排在最後。</summary>
    public IReadOnlyList<string> Employees => _employees ??= OrderedEmployees()
        .Select(e => e.Name)
        .ToList();

    /// <summary>只有科別是「組長」的人。「SW-應用系統主管」的下拉吃這一份。</summary>
    public IReadOnlyList<string> EmployeeManagers => _employeeManagers ??= OrderedEmployees()
        .Where(e => e.Section == Employee.ManagerSection)
        .Select(e => e.Name)
        .ToList();

    private IEnumerable<Employee> OrderedEmployees() => _db.Employees
        .OrderBy(e => e.TeamName == "").ThenBy(e => e.TeamName)
        .ThenBy(e => e.Section != Employee.ManagerSection).ThenBy(e => e.Name)
        .ToList();

    /// <summary>
    /// 委外廠商的建議清單。業務端決定廠商不進維護表——可以下拉也可以自行輸入，
    /// 自行輸入的名稱不回寫——所以這裡的建議直接取自資料現有的值。
    /// </summary>
    public IReadOnlyList<string> Vendors => _vendors ??= _db.InfoSystems
        .Select(s => s.SwVendor)
        .Where(v => v != "")
        .Distinct()
        .OrderBy(v => v)
        .ToList();

    /// <summary>
    /// 由業務端自行維護的欄位選項（<see cref="OptionCatalog"/> 登記的那些）。
    /// 同一個請求內每個欄位只查一次。
    /// </summary>
    public IReadOnlyList<string> OptionsFor(string field) =>
        _fieldOptions.TryGetValue(field, out var cached)
            ? cached
            : _fieldOptions[field] = _db.FieldOptionItems
                .Where(o => o.FieldName == field)
                .OrderBy(o => o.SortOrder).ThenBy(o => o.Id)
                .Select(o => o.Value)
                .ToList();

    /// <summary>單選下拉的捷徑，等同 <c>ItemsFor(OptionsFor(field), currentValue)</c>。</summary>
    public List<SelectListItem> OptionItemsFor(string field, string? currentValue) =>
        ItemsFor(OptionsFor(field), currentValue);

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

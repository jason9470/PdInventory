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
    private readonly ICurrentUser _currentUser;
    private List<string>? _departments;
    private List<string>? _opsStaff;
    private List<string>? _employees;
    private List<string>? _employeeManagers;
    private List<string>? _vendors;
    private Dictionary<string, string>? _employeeTeams;
    private List<(string Name, string Team)>? _employeesWithTeam;
    private List<(string Name, string Team)>? _managersWithTeam;
    private string? _currentTeamName;
    private readonly Dictionary<string, IReadOnlyList<string>> _fieldOptions = [];

    public LookupOptions(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

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

    /// <summary>備註是「組長」或「科長」的人。「SW-應用系統主管」的下拉吃這一份。</summary>
    public IReadOnlyList<string> EmployeeManagers => _employeeManagers ??= OrderedEmployees()
        .Where(e => e.IsManager)
        .Select(e => e.Name)
        .ToList();

    /// <summary>
    /// 組別、主管在前、姓名。IsManager 是依備註算出來的、不是資料庫欄位，
    /// EF 翻不成 SQL，因此先 ToList() 取回再排序。
    /// </summary>
    private IEnumerable<Employee> OrderedEmployees() => _db.Employees
        .ToList()
        .OrderBy(e => e.TeamName == "").ThenBy(e => e.TeamName)
        .ThenBy(e => !e.IsManager).ThenBy(e => e.Name);

    /// <summary>人員姓名與組別，順序同 <see cref="Employees"/>。人員單選下拉（_PersonSelect）用。</summary>
    public IReadOnlyList<(string Name, string Team)> EmployeesWithTeam => _employeesWithTeam ??=
        OrderedEmployees().Select(e => (e.Name, e.TeamName)).ToList();

    /// <summary>同上，只留備註是「組長」或「科長」的人。「SW-應用系統主管」的下拉吃這一份。</summary>
    public IReadOnlyList<(string Name, string Team)> EmployeeManagersWithTeam => _managersWithTeam ??=
        OrderedEmployees().Where(e => e.IsManager).Select(e => (e.Name, e.TeamName)).ToList();

    /// <summary>
    /// 人員姓名 → 組別。新增／編輯畫面用它把人員下拉限制在「SW-權責單位」那個組（0918）。
    /// 同名的人只會有一筆（姓名在人員表是唯一的）。
    /// </summary>
    public IReadOnlyDictionary<string, string> EmployeeTeams => _employeeTeams ??= _db.Employees
        .Select(e => new { e.Name, e.TeamName })
        .AsEnumerable()
        .ToDictionary(e => e.Name, e => e.TeamName);

    /// <summary>目前登入者的組別；人員表查不到（或沒有組別）時是空字串。</summary>
    public string CurrentTeamName => _currentTeamName ??= _db.Employees
        .Where(e => e.EmpNo == _currentUser.EmpNo)
        .Select(e => e.TeamName)
        .FirstOrDefault() ?? "";

    /// <summary>
    /// 「SW-權責單位」的選項（0918）：一般使用者只看得到自己的組，避免把資產掛到別組去；
    /// 管理者不受限制——部室主管與副主管本來就不屬於任何一組，照組別篩會一組都選不到。
    ///
    /// 目前值若不在清單中，<see cref="ItemsFor"/> 會自己補上並標示，既有資料不會因為
    /// 開了編輯畫面按存檔就被清掉。
    /// </summary>
    public IReadOnlyList<string> OwnerUnits
    {
        get
        {
            var all = OptionsFor(nameof(InfoSystem.SwOwnerUnit));
            if (_currentUser.IsAdmin) return all;

            var mine = CurrentTeamName;
            return all.Where(u => u == mine).ToList();
        }
    }

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

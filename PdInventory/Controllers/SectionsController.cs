using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Models.ViewModels;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>
/// 科別（側邊欄在「權限設定」底下，0918 前在「維護資料-共用」）。0917 起權限的核心：人員屬於哪個科別，就能修改那個科別負責的系統。
///
/// 「某個科負責哪些系統」只在這個畫面改。人員畫面只唯讀顯示結果，
/// 同一件事只有一個地方能改，才不會出現兩邊設定互相矛盾。
/// </summary>
[Authorize(Policy = Policies.Admin)]
public class SectionsController : Controller
{
    private readonly AppDbContext _db;
    public SectionsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.Scope = await SectionScope.LoadAsync(_db);
        ViewBag.PeopleCounts = await PeopleCountsAsync();
        return View(await _db.Sections.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync());
    }

    /// <summary>
    /// 負責系統圖：部室 → 組 → 科 → 系統。與人員的組織圖同一套畫法（_OrgTree），
    /// 範圍一律走 SectionScope，畫面上看到的就是權限實際生效的範圍。
    /// </summary>
    public async Task<IActionResult> Chart()
    {
        var sections = await _db.Sections.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync();
        var scope = await SectionScope.LoadAsync(_db);
        var allSystems = await _db.InfoSystems.OrderBy(s => s.SystemCode).ToListAsync();

        OrgUnit Office(Section s) => new(s.Id, s.Name, "不負責系統", []);
        // 組的名牌是底下各科的合計（組長能改的範圍）
        OrgUnit Team(Section s) => new(s.Id, s.Name, $"全組 {scope.SystemsOf(s.Id).Count} 套", []);
        OrgUnit Part(Section s)
        {
            var systems = scope.SystemsOf(s.Id);
            return new(s.Id, s.Name, $"{systems.Count} 套", systems
                .Select(x => new OrgItem(x.SystemName, Code: x.SystemCode))
                .ToList());
        }

        var parts = sections.Where(s => s.Kind == SectionKind.Section).ToList();
        var covered = parts.SelectMany(s => scope.CodesOf(s.Id)).ToHashSet();

        var model = new OrgChartViewModel
        {
            Offices = sections.Where(s => s.Kind == SectionKind.Office).Select(Office).ToList(),
            Teams = sections.Where(s => s.Kind == SectionKind.Team)
                .Select(t => new OrgTeam(Team(t), parts.Where(s => s.TeamName == t.TeamName).Select(Part).ToList()))
                .ToList(),
            Unassigned = allSystems.Where(s => !covered.Contains(s.SystemCode))
                .Select(s => $"{s.SystemCode} {s.SystemName}").ToList(),
            UnassignedLabel = "沒有任何科負責的系統，只有管理者能修改",
        };

        ViewBag.TotalSystems = allSystems.Count;
        ViewBag.CoveredSystems = allSystems.Count(s => covered.Contains(s.SystemCode));
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        await LoadFormAsync(null);
        return View("Form", new Section());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Section model, int[] systemIds)
    {
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync(null, systemIds);
            return View("Form", model);
        }

        _db.Sections.Add(model);
        await _db.SaveChangesAsync();
        await SyncSystemsAsync(model, systemIds);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"已新增科別「{model.Name}」";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.Sections.FindAsync(id);
        if (model is null) return NotFound();
        await LoadFormAsync(model);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Section model, int[] systemIds)
    {
        if (id != model.Id) return BadRequest();
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync(model, systemIds);
            return View("Form", model);
        }

        _db.Update(model);
        await SyncSystemsAsync(model, systemIds);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"已更新科別「{model.Name}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.Sections.FindAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));

        // 還有人掛在底下就不給刪：刪了那些人就瞬間失去所有修改權限，而且畫面上沒有任何提示
        var people = await _db.Employees.CountAsync(e => e.SectionId == id);
        if (people > 0)
        {
            TempData["Error"] = $"「{model.Name}」底下還有 {people} 個人，請先到人員表把他們改到別的科別再刪除。";
            return RedirectToAction(nameof(Index));
        }

        // 還負責著系統也不給刪：那些系統會少一個能修改的科，要刪就先在編輯畫面把勾選清掉，
        // 確認是有意為之
        var systems = await _db.SectionSystems.CountAsync(x => x.SectionId == id);
        if (systems > 0)
        {
            TempData["Error"] = $"「{model.Name}」還負責 {systems} 套系統，請先在編輯畫面取消勾選再刪除。";
            return RedirectToAction(nameof(Index));
        }

        _db.Sections.Remove(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已刪除科別「{model.Name}」";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 把勾選結果套用到對照表：只加新勾的、只刪取消勾的。
    /// 不是「科」類型就一律清空——組的範圍是算出來的、部室不負責系統，
    /// 留著舊的對照只會讓人以為它還有作用。
    /// </summary>
    private async Task SyncSystemsAsync(Section section, int[] systemIds)
    {
        var wanted = section.Kind == SectionKind.Section ? systemIds.ToHashSet() : [];

        // IgnoreQueryFilters：已軟刪除的系統若還有對照列，也要一起清掉，不然複合主鍵會撞
        var existing = await _db.SectionSystems.IgnoreQueryFilters()
            .Where(x => x.SectionId == section.Id)
            .ToListAsync();

        _db.SectionSystems.RemoveRange(existing.Where(x => !wanted.Contains(x.InfoSystemId)));

        var current = existing.Select(x => x.InfoSystemId).ToHashSet();
        foreach (var systemId in wanted.Where(systemId => !current.Contains(systemId)))
            _db.SectionSystems.Add(new SectionSystem { SectionId = section.Id, InfoSystemId = systemId });
    }

    private async Task LoadFormAsync(Section? section, int[]? postedSystemIds = null)
    {
        ViewBag.Systems = await _db.InfoSystems.OrderBy(s => s.SystemCode).ToListAsync();
        ViewBag.TeamNames = await _db.Sections
            .Where(s => s.TeamName != "")
            .OrderBy(s => s.SortOrder)
            .Select(s => s.TeamName)
            .Distinct()
            .ToListAsync();

        // 驗證沒過退回表單時，保留使用者剛才的勾選，而不是還原成資料庫的
        ViewBag.CheckedSystemIds = postedSystemIds is not null
            ? postedSystemIds.ToHashSet()
            : section is null
                ? new HashSet<int>()
                : (await _db.SectionSystems.Where(x => x.SectionId == section.Id)
                                           .Select(x => x.InfoSystemId).ToListAsync()).ToHashSet();

        ViewBag.PeopleCount = section is null ? 0 : await _db.Employees.CountAsync(e => e.SectionId == section.Id);
    }

    private async Task<Dictionary<int, int>> PeopleCountsAsync() =>
        await _db.Employees
            .Where(e => e.SectionId != null)
            .GroupBy(e => e.SectionId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

    private async Task ValidateUniqueNameAsync(Section model)
    {
        if (await _db.Sections.AnyAsync(s => s.Name == model.Name && s.Id != model.Id))
            ModelState.AddModelError(nameof(Section.Name), $"科別「{model.Name}」已存在");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>人員（共用維護檔）。資訊系統開發一部的組織名冊。</summary>
[Authorize(Policy = Policies.Admin)]
public class EmployeesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly UserProvisioning _provisioning;

    public EmployeesController(AppDbContext db, ICurrentUser currentUser,
                               UserProvisioning provisioning)
    {
        _db = db;
        _currentUser = currentUser;
        _provisioning = provisioning;
    }

    /// <param name="section">只看某個科別的人（科別畫面的「N 人」連過來）。</param>
    public async Task<IActionResult> Index(string? q, int? section)
    {
        var query = _db.Employees.Include(e => e.Section).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(e => e.EmpNo.Contains(q)
                                  || e.Name.Contains(q)
                                  || e.DepartmentName.Contains(q)
                                  || e.TeamName.Contains(q)
                                  || (e.Section != null && e.Section.Name.Contains(q))
                                  || e.Remark.Contains(q));

        if (section is not null)
            query = query.Where(e => e.SectionId == section);

        ViewBag.Query = q;
        ViewBag.SectionFilter = section;
        ViewBag.UsageCounts = await LookupUsage.CountsAsync(_db, UsageKind.Employee);
        ViewBag.Scope = await SectionScope.LoadAsync(_db);
        ViewBag.Sections = await OrderedSectionsAsync();

        // 「可修改系統」那一欄要分得出管理者（不受科別限制），也要能連到權限設定畫面
        var users = await _db.AppUsers.Select(u => new { u.EmpNo, u.Id, u.Role }).ToListAsync();
        ViewBag.UserIds = users.ToDictionary(u => u.EmpNo, u => u.Id);
        ViewBag.Admins = users.Where(u => u.Role == UserRole.Admin).Select(u => u.EmpNo).ToHashSet();

        // 依科別的排序（組在前、各科在後）；沒有科別的排在最後。
        // IsManager 是算出來的（看備註），不能翻成 SQL，因此先取回再排
        var rows = await query.ToListAsync();
        return View(rows.OrderBy(e => e.Section is null)
                        .ThenBy(e => e.Section?.SortOrder ?? int.MaxValue)
                        .ThenBy(e => !e.IsManager)
                        .ThenBy(e => e.Name)
                        .ToList());
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Sections = await OrderedSectionsAsync();
        ViewBag.Scope = await SectionScope.LoadAsync(_db);
        return View("Form", new Employee());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee model)
    {
        model.EmpNo = EmpNo.Normalize(model.EmpNo);
        await ValidateUniqueNameAsync(model);
        await ApplySectionAsync(model);
        if (!ModelState.IsValid)
        {
            ViewBag.Sections = await OrderedSectionsAsync();
            ViewBag.Scope = await SectionScope.LoadAsync(_db);
            return View("Form", model);
        }

        // 同一個員編以前被停用過：復原原本那筆並套上新填的內容，
        // 而不是再插一筆同員編的——否則人員表會出現一停用、一有效的兩列。
        var revived = string.IsNullOrWhiteSpace(model.EmpNo)
            ? null
            : await _provisioning.FindDeactivatedEmployeeAsync(model.EmpNo);

        if (revived is not null)
        {
            UserProvisioning.RestoreEmployee(revived);
            InfoSystemBlocks.Copy(revived, model);
            model = revived;
        }
        else
        {
            _db.Employees.Add(model);
        }
        // 建人員的同時把使用者帳號也建起來，管理者接著到權限設定調角色即可；
        // 那個人之後登入就直接對應到已經設好的權限，不必等他先登入一次。
        var user = await _provisioning.EnsureUserForEmployeeAsync(model);
        await _db.SaveChangesAsync();

        var verb = revived is null ? "已新增" : "已復原先前刪除的";
        TempData["Message"] = user is null
            ? $"{verb}人員「{model.Label}」。因為沒有填員編，沒有一併建立可登入的帳號。"
            : $"{verb}人員「{model.Label}」，權限設定的帳號也一併可用了。";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.Employees.FindAsync(id);
        if (model is null) return NotFound();
        ViewBag.UsedBy = await CountUsageAsync(model.Name);
        ViewBag.Sections = await OrderedSectionsAsync();
        ViewBag.Scope = await SectionScope.LoadAsync(_db);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Employee model)
    {
        if (id != model.Id) return BadRequest();
        // 手動輸入 183253 也要存成 0183253，維護畫面不該是格式不一致的來源
        model.EmpNo = EmpNo.Normalize(model.EmpNo);
        await ValidateUniqueNameAsync(model);
        await ApplySectionAsync(model);
        if (!ModelState.IsValid)
        {
            ViewBag.UsedBy = await CountUsageAsync(model.Name);
            ViewBag.Sections = await OrderedSectionsAsync();
            ViewBag.Scope = await SectionScope.LoadAsync(_db);
            return View("Form", model);
        }
        _db.Update(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"已更新人員「{model.Label}」";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.Employees.FindAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));

        var used = await CountUsageAsync(model.Name);
        if (used > 0)
        {
            TempData["Error"] = $"「{model.Name}」還被 {used} 筆資料使用中，請先改掉那些資料再刪除。";
            return RedirectToAction(nameof(Index));
        }

        // 從人員表刪除會繞過權限設定畫面的兩道保護，這裡補上
        var blocked = await _provisioning.DeactivateBlockReason(model, _currentUser.EmpNo);
        if (blocked is not null)
        {
            TempData["Error"] = blocked;
            return RedirectToAction(nameof(Index));
        }

        // 軟刪除：人員表與使用者帳號一起加註記。資料不會真的從資料庫消失，
        // 但全域查詢篩選讓他從清單、所有下拉與權限設定中消失，也不能再登入。
        await _provisioning.DeactivateAsync(model, _currentUser.Name);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"已刪除人員「{model.Label}」，其登入帳號與權限一併停用。";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 有多少筆資料用到這個人。六個對照欄位裡有三個是多選（以 / 分隔），
    /// 不能用等號比對，理由同 DepartmentsController。同一筆多個欄位都是他只算一筆。
    /// </summary>
    /// <summary>與清單上的「使用中」和明細視窗走同一支，三處各寫一套遲早會對不上。</summary>
    private Task<int> CountUsageAsync(string name) =>
        LookupUsage.CountAsync(_db, UsageKind.Employee, name);

    /// <summary>
    /// 組別跟著科別走：選了科別就以科別表上的組別為準，畫面送來的值不採信。
    /// 兩者分開填遲早會出現「科別在開發一組、組別卻寫開發二組」。
    /// 沒選科別的（還沒分派、或自動建檔的人）組別維持原值。
    /// </summary>
    private async Task ApplySectionAsync(Employee model)
    {
        if (model.SectionId is null) return;

        var section = await _db.Sections.FindAsync(model.SectionId);
        if (section is null)
        {
            ModelState.AddModelError(nameof(Employee.SectionId), "選擇的科別已不存在，請重新選擇。");
            return;
        }

        model.TeamName = section.TeamName;
    }

    private Task<List<Section>> OrderedSectionsAsync() =>
        _db.Sections.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync();

    private async Task ValidateUniqueNameAsync(Employee model)
    {
        if (await _db.Employees.AnyAsync(e => e.Name == model.Name && e.Id != model.Id))
            ModelState.AddModelError(nameof(Employee.Name), $"人員「{model.Name}」已存在");
    }
}

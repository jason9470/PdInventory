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

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Employees.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(e => e.EmpNo.Contains(q)
                                  || e.Name.Contains(q)
                                  || e.DepartmentName.Contains(q)
                                  || e.TeamName.Contains(q)
                                  || e.Section.Contains(q)
                                  || e.Remark.Contains(q));

        ViewBag.Query = q;
        // 組別待補的排在最後：那些是資料裡有、名冊沒有的，等甲方補
        return View(await query.OrderBy(e => e.TeamName == "")
                               .ThenBy(e => e.TeamName)
                               .ThenBy(e => e.Section != Employee.ManagerSection)
                               .ThenBy(e => e.Name)
                               .ToListAsync());
    }

    public IActionResult Create() => View("Form", new Employee());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee model)
    {
        model.EmpNo = EmpNo.Normalize(model.EmpNo);
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid) return View("Form", model);

        _db.Employees.Add(model);
        // 建人員的同時把使用者帳號也建起來，管理者接著到權限設定調角色即可；
        // 那個人之後登入就直接對應到已經設好的權限，不必等他先登入一次。
        var user = await _provisioning.EnsureUserForEmployeeAsync(model);
        await _db.SaveChangesAsync();

        TempData["Message"] = user is null
            ? $"已新增人員「{model.Label}」。因為沒有填員編，沒有一併建立可登入的帳號。"
            : $"已新增人員「{model.Label}」，並在權限設定建立帳號（預設為資產負責人）。";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.Employees.FindAsync(id);
        if (model is null) return NotFound();
        ViewBag.UsedBy = await CountUsageAsync(model.Name);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Employee model)
    {
        if (id != model.Id) return BadRequest();
        // 手動輸入 183253 也要存成 0183253，維護畫面不該是格式不一致的來源
        model.EmpNo = EmpNo.Normalize(model.EmpNo);
        await ValidateUniqueNameAsync(model);
        if (!ModelState.IsValid)
        {
            ViewBag.UsedBy = await CountUsageAsync(model.Name);
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
    private async Task<int> CountUsageAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;

        var systems = await _db.InfoSystems
            .Select(s => new { s.SwAppManager, s.SwAppMaintainer, s.SwAppMaintainerDeputy,
                               s.SwDeveloper, s.SwReviewer })
            .ToListAsync();
        var dataAssets = await _db.DataAssets.Select(d => d.DaReviewer).ToListAsync();

        return systems.Count(s => MultiValue.Contains(s.SwAppManager, name)
                               || MultiValue.Contains(s.SwAppMaintainer, name)
                               || MultiValue.Contains(s.SwAppMaintainerDeputy, name)
                               || MultiValue.Contains(s.SwDeveloper, name)
                               || MultiValue.Contains(s.SwReviewer, name))
             + dataAssets.Count(d => MultiValue.Contains(d, name));
    }

    private async Task ValidateUniqueNameAsync(Employee model)
    {
        if (await _db.Employees.AnyAsync(e => e.Name == model.Name && e.Id != model.Id))
            ModelState.AddModelError(nameof(Employee.Name), $"人員「{model.Name}」已存在");
    }
}

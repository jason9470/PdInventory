using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Models.ViewModels;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>
/// 人員（側邊欄：權限設定 → 人員）。資訊系統開發一部的組織名冊，同時也是登入帳號與角色的設定畫面。
///
/// 0918 以前人員（維護資料-共用）與權限設定是兩個畫面：組織資料在這裡、角色在權限設定，
/// 而兩張表一人一筆、以員編相認，畫面之間只能互相連來連去。合併後一個人的所有設定都在同一張表單：
///   人屬於哪個科（決定能改哪些系統）、角色（決定能做哪些動作）。
/// 科負責哪些系統仍在科別畫面設定。帳號跟著人員走的規則見 <see cref="UserProvisioning"/>。
/// </summary>
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
    /// <param name="role">只看某個角色的人。</param>
    public async Task<IActionResult> Index(string? q, int? section, UserRole? role)
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

        // 角色在帳號那一邊，以員編對上。沒有員編的人沒有帳號，選了角色篩選就不會出現
        var accounts = await _db.AppUsers.ToDictionaryAsync(u => u.EmpNo);
        if (role is not null)
        {
            var empNos = accounts.Values.Where(u => u.Role == role).Select(u => u.EmpNo).ToList();
            query = query.Where(e => empNos.Contains(e.EmpNo));
        }

        ViewBag.Query = q;
        ViewBag.SectionFilter = section;
        ViewBag.RoleFilter = role;
        ViewBag.UsageCounts = await LookupUsage.CountsAsync(_db, UsageKind.Employee);
        ViewBag.Scope = await SectionScope.LoadAsync(_db);
        ViewBag.Sections = await OrderedSectionsAsync();
        ViewBag.Accounts = accounts;

        // 依科別的排序（組在前、各科在後）；沒有科別的排在最後。
        // IsManager 是算出來的（看備註），不能翻成 SQL，因此先取回再排
        var rows = await query.ToListAsync();
        return View(rows.OrderBy(e => e.Section is null)
                        .ThenBy(e => e.Section?.SortOrder ?? int.MaxValue)
                        .ThenBy(e => !e.IsManager)
                        .ThenBy(e => e.Name)
                        .ToList());
    }

    /// <summary>
    /// 組織圖：部室 → 組 → 科 → 人。全部由科別表與人員表畫出來，不另外維護一份，
    /// 否則人換了科、圖卻沒跟著改，兩邊很快就對不上。畫法與科別的負責系統圖共用 _OrgTree。
    /// </summary>
    public async Task<IActionResult> OrgChart()
    {
        var sections = await _db.Sections.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync();
        // IsManager 是算出來的（看備註），不能翻成 SQL，因此先取回再分組
        var employees = await _db.Employees.ToListAsync();
        var people = employees
            .Where(e => e.SectionId is not null)
            .GroupBy(e => e.SectionId!.Value)
            .ToDictionary(g => g.Key, g => g
                .OrderBy(e => !e.IsManager).ThenBy(e => e.Name)
                .ToList());

        List<Employee> Of(Section s) => people.GetValueOrDefault(s.Id) ?? [];

        // 部室與組的名牌寫人名（部室主管、組長）；科底下逐一列出，主管排前面、加粗並標上職稱
        OrgUnit Named(Section s) => new(s.Id, s.Name,
            Of(s).Count == 0 ? "（未指派）" : string.Join("、", Of(s).Select(e => e.Name)), []);
        OrgUnit Listed(Section s) => new(s.Id, s.Name, "", Of(s)
            .Select(e => new OrgItem(e.Name, Badge: e.IsManager ? e.Remark.Trim() : null, Strong: e.IsManager))
            .ToList());

        var model = new OrgChartViewModel
        {
            Offices = sections.Where(s => s.Kind == SectionKind.Office).Select(Named).ToList(),
            // 組底下的科以 TeamName 對上（與 SectionScope 算組的範圍同一個規則）
            Teams = sections.Where(s => s.Kind == SectionKind.Team)
                .Select(t => new OrgTeam(Named(t), sections
                    .Where(s => s.Kind == SectionKind.Section && s.TeamName == t.TeamName)
                    .Select(Listed).ToList()))
                .ToList(),
            Unassigned = employees.Where(e => e.SectionId is null).Select(e => e.Name).OrderBy(n => n).ToList(),
            UnassignedLabel = "沒有指定科別的人",
        };

        ViewBag.TotalPeople = employees.Count;
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var model = new Employee();
        await PrepareFormAsync(model, null, UserRole.AssetOwner);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee model, UserRole role = UserRole.AssetOwner)
    {
        model.EmpNo = EmpNo.Normalize(model.EmpNo);
        await ValidateUniqueNameAsync(model);
        await ValidateUniqueEmpNoAsync(model);
        await ApplySectionAsync(model);
        if (!ModelState.IsValid)
        {
            await PrepareFormAsync(model, null, role);
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

        // 建人員的同時把帳號也建起來，角色就是表單上選的；
        // 那個人之後登入就直接對應到已經設好的權限，不必等他先登入一次。
        var user = await _provisioning.SyncUserForEmployeeAsync(model, "", _currentUser.Name);
        if (user is not null) user.Role = role;
        await _db.SaveChangesAsync();

        var verb = revived is null ? "已新增" : "已復原先前刪除的";
        TempData["Message"] = user is null
            ? $"{verb}人員「{model.Label}」。因為沒有填員編，沒有一併建立可登入的帳號。"
            : $"{verb}人員「{model.Label}」，登入帳號一併建立，角色為{RoleDisplay.Name(user.Role)}。";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _db.Employees.FindAsync(id);
        if (model is null) return NotFound();

        var account = await AccountOfAsync(model.EmpNo);
        await PrepareFormAsync(model, account, account?.Role ?? UserRole.AssetOwner);
        return View("Form", model);
    }

    /// <param name="role">表單上選的角色。</param>
    /// <param name="accountRowVersion">
    /// 畫面載入當下帳號的並行權杖。人員表本身沒有並行控制，角色有——
    /// 兩位管理者同時改同一個人的角色時，後存的要被擋下，而不是悄悄蓋掉前一位。
    /// </param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Employee model, UserRole role = UserRole.AssetOwner,
                                          Guid? accountRowVersion = null)
    {
        if (id != model.Id) return BadRequest();

        var original = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (original is null) return NotFound();
        var account = await AccountOfAsync(original.EmpNo);

        // 手動輸入 183253 也要存成 0183253，維護畫面不該是格式不一致的來源
        model.EmpNo = EmpNo.Normalize(model.EmpNo);
        await ValidateUniqueNameAsync(model);
        await ValidateUniqueEmpNoAsync(model);
        await ApplySectionAsync(model);

        var empNoChanged = !string.Equals(original.EmpNo, model.EmpNo, StringComparison.Ordinal);
        if (empNoChanged && string.Equals(original.EmpNo, _currentUser.EmpNo, StringComparison.Ordinal))
        {
            // 登入身分是以員編記在 Cookie 裡的，改掉自己的員編等於當場把自己登出、而且對不回帳號
            ModelState.AddModelError(nameof(Employee.EmpNo), "不能修改自己的員編，請由另一位管理者操作。");
        }
        else if (empNoChanged && string.IsNullOrWhiteSpace(model.EmpNo))
        {
            // 清空員編會停用帳號，與刪除同樣要擋「刪掉最後一位管理者」
            var blocked = await _provisioning.DeactivateBlockReason(original, _currentUser.EmpNo);
            if (blocked is not null)
                ModelState.AddModelError(nameof(Employee.EmpNo), "清空員編會停用這個人的登入帳號，而他是最後一位管理者，請先指派另一位管理者。");
        }

        if (account is not null && !string.IsNullOrWhiteSpace(model.EmpNo))
        {
            var blocked = await _provisioning.RoleChangeBlockReason(account, role, _currentUser.EmpNo);
            if (blocked is not null) ModelState.AddModelError("role", blocked);
        }

        if (!ModelState.IsValid)
        {
            await PrepareFormAsync(model, account, role);
            return View("Form", model);
        }

        _db.Update(model);
        var user = await _provisioning.SyncUserForEmployeeAsync(model, original.EmpNo, _currentUser.Name);
        if (user is not null)
        {
            // 同一個帳號才比對權杖；改員編而換到另一個帳號時，畫面上那個權杖本來就不是它的
            if (account is not null && user.Id == account.Id && accountRowVersion is not null)
                _db.Entry(user).Property(u => u.RowVersion).OriginalValue = accountRowVersion.Value;
            user.Role = role;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "這個人的角色在你編輯期間已被其他管理者修改，畫面已重新載入最新內容，請確認後再存一次。";
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["Message"] = user is null
            ? $"已更新人員「{model.Label}」（沒有員編，沒有登入帳號）"
            : $"已更新人員「{model.Label}」，角色：{RoleDisplay.Name(user.Role)}";
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

        // 刪除會停用帳號，同樣要擋「刪掉自己」與「刪掉最後一位管理者」
        var blocked = await _provisioning.DeactivateBlockReason(model, _currentUser.EmpNo);
        if (blocked is not null)
        {
            TempData["Error"] = blocked;
            return RedirectToAction(nameof(Index));
        }

        // 軟刪除：人員表與使用者帳號一起加註記。資料不會真的從資料庫消失，
        // 但全域查詢篩選讓他從清單與所有下拉中消失，也不能再登入。
        await _provisioning.DeactivateAsync(model, _currentUser.Name);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"已刪除人員「{model.Label}」，其登入帳號與權限一併停用。";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>表單畫面要的東西：科別下拉、科別推出的系統範圍、帳號與角色。</summary>
    private async Task PrepareFormAsync(Employee model, AppUser? account, UserRole role)
    {
        ViewBag.UsedBy = model.Id == 0 ? 0 : await CountUsageAsync(model.Name);
        ViewBag.Sections = await OrderedSectionsAsync();
        ViewBag.Scope = await SectionScope.LoadAsync(_db);
        ViewBag.TotalSystems = await _db.InfoSystems.CountAsync();
        ViewBag.Account = account;
        ViewBag.Role = role;
        // 表單上要顯示的是「存檔前」的科別，Include 不到時補查一次
        ViewBag.Section = model.SectionId is null ? null : await _db.Sections.FindAsync(model.SectionId);
    }

    /// <summary>員編對應的帳號（不含已停用）。沒有員編就沒有帳號。</summary>
    private Task<AppUser?> AccountOfAsync(string empNo) => string.IsNullOrWhiteSpace(empNo)
        ? Task.FromResult<AppUser?>(null)
        : _db.AppUsers.FirstOrDefaultAsync(u => u.EmpNo == empNo);

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

    /// <summary>
    /// 員編不可重複（留空的除外）。兩個人同一個員編，登入時就不知道對應到誰，
    /// 帳號也只有一個、角色會互相蓋掉。
    /// </summary>
    private async Task ValidateUniqueEmpNoAsync(Employee model)
    {
        if (string.IsNullOrWhiteSpace(model.EmpNo)) return;

        var other = await _db.Employees.FirstOrDefaultAsync(e => e.EmpNo == model.EmpNo && e.Id != model.Id);
        if (other is not null)
            ModelState.AddModelError(nameof(Employee.EmpNo), $"員編 {model.EmpNo} 已經是「{other.Name}」的");
    }
}

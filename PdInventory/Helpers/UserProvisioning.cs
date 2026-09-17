using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>
/// 人員表（<see cref="Employee"/>）與使用者帳號（<see cref="AppUser"/>）的對接。
///
/// 兩張表以**員工編號**相認，各自負責不同的事：
///   人員表   —— 組織名冊，也是各表單人員欄位下拉的來源，姓名就是欄位裡存的值。
///   使用者帳號 —— 能不能登入、是什麼角色。能修改哪些系統由人員表的科別決定。
///
/// 兩種建立途徑都會讓兩邊同時存在：
///   (1) 使用者自己先登入 —— 人員表沒有他就自動建一筆（部門與備註填固定值待補），
///       帳號給一般使用者。
///   (2) 管理者先在人員表建檔 —— 這裡同時補上帳號，角色在同一張表單上指定；
///       那個人之後登入就直接對應到已經設好的權限。
///
/// 0918 起人員與權限設定合併成同一個畫面（權限設定 → 人員），改員編、補員編時
/// 帳號也在這裡跟著處理（<see cref="SyncUserForEmployeeAsync"/>），不再只靠啟動時補。
///
/// 刪除一律是軟刪除，而且兩邊一起：各表單存的是姓名文字，實體真的消失之後
/// 那些欄位會變成查不出來源的孤兒值，軌跡欄位也一樣認不出人。
///
/// 這些規則集中在這裡，是因為登入（AccountController）與人員維護
/// （EmployeesController）兩邊都要用；散在兩個控制器很快就會走樣。
/// </summary>
public sealed class UserProvisioning
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserProvisioning> _logger;

    public UserProvisioning(AppDbContext db, ILogger<UserProvisioning> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>被停用的人不得登入，這是回傳給他看的訊息。</summary>
    public const string DeactivatedMessage = "這個帳號已被停用，請洽系統管理者。";

    /// <summary>
    /// 人員表建檔後補上對應的使用者帳號。已存在就沿用（被停用過的會一併復原），
    /// 因此管理者「刪除後再新增同一個人」不會撞到編號唯一索引。
    /// </summary>
    /// <returns>對應的帳號；<paramref name="employee"/> 沒有員工編號時回傳 null——
    /// 沒有編號就無從對應登入身分，例如名冊裡本來就缺編號的人與幾家委外廠商。</returns>
    public async Task<AppUser?> EnsureUserForEmployeeAsync(Employee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.EmpNo)) return null;

        var user = await FindUserAsync(employee.EmpNo);
        if (user is null)
        {
            user = new AppUser
            {
                EmpNo = employee.EmpNo,
                EmpName = employee.Name,
                Role = UserRole.AssetOwner,
            };
            _db.AppUsers.Add(user);
        }
        else if (user.IsDeleted)
        {
            Restore(user);
            user.EmpName = employee.Name;
        }

        return user;
    }

    /// <summary>
    /// 人員資料存檔後讓帳號跟上：改了員編、原本沒員編後來補上、姓名改了，都在這裡處理。
    ///
    /// 0918 以前人員表改員編不會動到帳號，舊員編的帳號就這樣留著、新員編要等下次啟動才補建，
    /// 角色也就跟著斷掉（黃鈺棠 0012478 → 0014078 就是這樣多出一個帳號）。
    ///
    /// 規則：
    ///   新員編已經有帳號（含停用過的）→ 沿用那個帳號（停用過的復原），舊員編的帳號停用。
    ///   新員編沒有帳號、舊員編有       → 舊帳號直接改成新員編，角色與登入紀錄都留著。
    ///   兩邊都沒有                     → 建一個新帳號。
    ///   員編被清空                     → 舊帳號停用（沒有編號就無從對應登入身分）。
    /// </summary>
    /// <param name="oldEmpNo">存檔前的員編；新增時傳空字串。</param>
    /// <returns>這個人現在對應的帳號；沒有員編時回傳 null。</returns>
    public async Task<AppUser?> SyncUserForEmployeeAsync(Employee employee, string oldEmpNo, string actor)
    {
        var old = string.IsNullOrWhiteSpace(oldEmpNo) ? null : await FindUserAsync(oldEmpNo);

        if (string.IsNullOrWhiteSpace(employee.EmpNo))
        {
            if (old is not null && !old.IsDeleted) Stamp(old, actor);
            return null;
        }

        AppUser user;
        var target = await FindUserAsync(employee.EmpNo);
        if (target is not null)
        {
            if (target.IsDeleted) Restore(target);
            if (old is not null && old.Id != target.Id && !old.IsDeleted) Stamp(old, actor);
            user = target;
        }
        else if (old is not null)
        {
            if (old.IsDeleted) Restore(old);
            old.EmpNo = employee.EmpNo;
            user = old;
        }
        else
        {
            user = new AppUser { EmpNo = employee.EmpNo, Role = UserRole.AssetOwner };
            _db.AppUsers.Add(user);
        }

        user.EmpName = employee.Name;
        return user;
    }

    /// <summary>
    /// 改角色前的把關：不能調降自己，也不能調降最後一位管理者——兩種都會讓系統再也沒有人能管理。
    /// 回傳 null 代表允許，否則回傳要顯示給使用者的原因。
    /// </summary>
    public async Task<string?> RoleChangeBlockReason(AppUser user, UserRole role, string currentEmpNo)
    {
        if (user.Role != UserRole.Admin || role == UserRole.Admin) return null;

        if (string.Equals(user.EmpNo, currentEmpNo, StringComparison.Ordinal))
            return "不能調降自己的角色，請由另一位管理者操作。";

        var admins = await _db.AppUsers.CountAsync(u => u.Role == UserRole.Admin);
        return admins <= 1 ? "系統至少要保留一位管理者，無法調降這個帳號。" : null;
    }

    /// <summary>
    /// 人員表裡這個員工編號是不是已經停用過了。管理者「刪除後再新增同一個人」時，
    /// 應該把原本那筆復原（連同組別、科別、備註），而不是再插一筆同員編的。
    /// </summary>
    public Task<Employee?> FindDeactivatedEmployeeAsync(string empNo) => _db.Employees
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(e => e.EmpNo == empNo && e.IsDeleted);

    /// <summary>停用過的人員復原。</summary>
    public static void RestoreEmployee(Employee employee) => Restore(employee);

    /// <summary>
    /// 登入時確保人員表有這個人。已停用回傳 null，呼叫端要拒絕登入。
    /// </summary>
    public async Task<Employee?> EnsureEmployeeForLoginAsync(EmployeeInfo info)
    {
        var employee = await FindEmployeeAsync(info.EmpNo);
        if (employee is not null) return employee.IsDeleted ? null : employee;

        // 名冊裡本來就有這個人、只是還沒有員工編號（甲方名冊有兩位是這樣）：
        // 補上編號即可，不要再插一筆同名的——姓名是唯一的，插了會直接失敗。
        if (!string.IsNullOrWhiteSpace(info.EmpName))
        {
            var byName = await _db.Employees.IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Name == info.EmpName);

            if (byName is not null)
            {
                if (byName.IsDeleted) return null;

                if (string.IsNullOrWhiteSpace(byName.EmpNo))
                {
                    byName.EmpNo = info.EmpNo;
                    _logger.LogInformation("人員「{Name}」原本沒有員工編號，登入時補上 {EmpNo}",
                                           info.EmpName, info.EmpNo);
                    return byName;
                }

                // 同名但編號不同：可能真的有兩個同名的人，也可能是資料有誤。
                // 這裡不硬插一筆去撞唯一索引，也不擋登入，交給管理者處理人員表。
                _logger.LogWarning("人員「{Name}」已存在但員工編號是 {Existing}，登入者是 {Incoming}，"
                                 + "未自動建立人員資料，請管理者確認人員表",
                                   info.EmpName, byName.EmpNo, info.EmpNo);
                return null;
            }
        }

        // 姓名是人員表的主識別，也是各表單欄位裡存的值，沒有姓名就不要建一筆空的——
        // 姓名欄有唯一索引，第二個沒姓名的人還會直接撞號。正式的 LDAP 一定回得出姓名，
        // 會走到這裡的是模擬模式沒有輸入姓名的情況。
        if (string.IsNullOrWhiteSpace(info.EmpName))
        {
            _logger.LogWarning("員工目錄沒有回傳 {EmpNo} 的姓名，未自動建立人員資料", info.EmpNo);
            return null;
        }

        employee = new Employee
        {
            EmpNo = info.EmpNo,
            Name = info.EmpName,
            DepartmentName = Employee.DefaultDepartment,
            Remark = Employee.AutoCreatedRemark,
        };
        _db.Employees.Add(employee);
        return employee;
    }

    /// <summary>
    /// 停用前的把關。回傳 null 代表允許，否則回傳要顯示給使用者的原因。
    ///
    /// 改角色時擋著「降自己的角色」與「降掉最後一位管理者」（<see cref="RoleChangeBlockReason"/>），
    /// 但刪除是另一條路、繞過了那兩道檢查——而管理者通常不會出現在應用系統主管、
    /// 維護人員那些欄位裡，引用數是 0，刪除鈕根本不會擋他。只剩一位管理者又被刪掉，
    /// 就再也沒有人進得了權限設定了。
    /// </summary>
    public async Task<string?> DeactivateBlockReason(Employee employee, string currentEmpNo)
    {
        if (string.IsNullOrWhiteSpace(employee.EmpNo)) return null;

        if (string.Equals(employee.EmpNo, currentEmpNo, StringComparison.Ordinal))
            return "不能刪除自己，請由另一位管理者操作。";

        var user = await FindUserAsync(employee.EmpNo);
        if (user is null || user.IsDeleted || user.Role != UserRole.Admin) return null;

        var admins = await _db.AppUsers.CountAsync(u => u.Role == UserRole.Admin);
        return admins <= 1
            ? "系統至少要保留一位管理者，無法刪除這個人員——請先指派另一位管理者。"
            : null;
    }

    /// <summary>
    /// 停用一個人：人員表與使用者帳號一起加註記。兩邊都不會真的從資料庫刪掉，
    /// 但全域查詢篩選會讓他從清單、下拉與權限設定中消失，也不能再登入。
    /// </summary>
    public async Task DeactivateAsync(Employee employee, string actor)
    {
        Stamp(employee, actor);

        if (string.IsNullOrWhiteSpace(employee.EmpNo)) return;

        var user = await FindUserAsync(employee.EmpNo);
        if (user is not null && !user.IsDeleted) Stamp(user, actor);
    }

    /// <summary>連同已停用的一起找——復原與擋登入都需要看得到被刪掉的那些。</summary>
    private Task<AppUser?> FindUserAsync(string empNo) => _db.AppUsers
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(u => u.EmpNo == empNo);

    private Task<Employee?> FindEmployeeAsync(string empNo) => string.IsNullOrWhiteSpace(empNo)
        ? Task.FromResult<Employee?>(null)
        : _db.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.EmpNo == empNo);

    /// <summary>登入時用：這個編號的帳號存在但已停用嗎。</summary>
    public async Task<bool> IsDeactivatedAsync(string empNo)
    {
        var user = await FindUserAsync(empNo);
        if (user is not null && user.IsDeleted) return true;

        var employee = await FindEmployeeAsync(empNo);
        return employee is not null && employee.IsDeleted;
    }

    private static void Stamp(ISoftDeletable target, string actor)
    {
        target.IsDeleted = true;
        target.DeletedAt = DateTime.Now;
        target.DeletedBy = actor;
    }

    private static void Restore(ISoftDeletable target)
    {
        target.IsDeleted = false;
        target.DeletedAt = null;
        target.DeletedBy = "";
    }
}

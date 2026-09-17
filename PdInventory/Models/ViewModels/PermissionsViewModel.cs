using PdInventory.Helpers;

namespace PdInventory.Models.ViewModels;

/// <summary>
/// 權限設定畫面：左邊使用者名單，右邊被選中那位的角色，以及他能修改哪些系統。
///
/// 0917 起「能修改哪些系統」由人員表的科別推導，這個畫面只唯讀呈現結果；
/// 唯一能改的是角色。
/// </summary>
public class PermissionsViewModel
{
    /// <summary>
    /// 所有已建檔的使用者，依科別排序。由管理者在人員表新增時一併建立，或那個人第一次登入時自動建檔。
    /// </summary>
    public List<AppUser> Users { get; set; } = [];

    /// <summary>員工編號 → 人員表那一筆（含科別）。沒建到人員表的查不到。</summary>
    public Dictionary<string, Employee> Employees { get; set; } = [];

    public Employee? EmployeeOf(AppUser user) =>
        Employees.GetValueOrDefault(user.EmpNo);

    public SectionScope Scope { get; set; } = null!;

    /// <summary>系統總數，管理者那邊顯示「全部 N 套」用。</summary>
    public int TotalSystems { get; set; }

    /// <summary>目前選中的使用者；還沒點任何人時為 null。</summary>
    public AppUser? Selected { get; set; }

    /// <summary>某位使用者能修改幾套系統。管理者回傳 null（不受科別限制）。</summary>
    public int? EditableCount(AppUser user) =>
        user.Role == UserRole.Admin ? null : Scope.SystemsOf(EmployeeOf(user)?.SectionId).Count;
}

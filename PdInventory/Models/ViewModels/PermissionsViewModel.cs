namespace PdInventory.Models.ViewModels;

/// <summary>權限設定畫面：左邊使用者名單，右邊被選中那位的角色與資產授權。</summary>
public class PermissionsViewModel
{
    /// <summary>
    /// 所有已建檔的使用者。由管理者在人員表新增時一併建立，或那個人第一次登入時自動建檔。
    /// </summary>
    public List<AppUser> Users { get; set; } = [];

    /// <summary>員工編號 → 人員表那一筆，供畫面顯示組別與科別。沒建到人員表的查不到。</summary>
    public Dictionary<string, Employee> Employees { get; set; } = [];

    public Employee? EmployeeOf(AppUser user) =>
        Employees.GetValueOrDefault(user.EmpNo);

    /// <summary>目前選中的使用者；還沒點任何人時為 null。</summary>
    public AppUser? Selected { get; set; }

    /// <summary>系統中所有資訊資產，供勾選。</summary>
    public List<InfoSystem> Assets { get; set; } = [];

    /// <summary>選中的使用者名下已授權的資產 Id。</summary>
    public HashSet<int> OwnedAssetIds { get; set; } = [];

    public bool IsOwned(int assetId) => OwnedAssetIds.Contains(assetId);
}

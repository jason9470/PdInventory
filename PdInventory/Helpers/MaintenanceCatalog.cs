namespace PdInventory.Helpers;

/// <summary>一個維護項目：對應的控制器與側邊欄顯示名稱。</summary>
public record MaintenanceItem(string Controller, string Title);

/// <summary>
/// 維護項目所屬的表單。<paramref name="Key"/> 與 <see cref="ExportCatalog.Lists"/>
/// 的清單代碼一致，唯一的例外是 <see cref="SharedKey"/>。
/// </summary>
public record MaintenanceGroup(string Key, string Title, IReadOnlyList<MaintenanceItem> Items);

/// <summary>
/// 側邊欄「維護資料」的三層結構：維護資料 → 六張表單（外加「共用」）→ 各表單的維護項目。
///
/// 集中在這裡而不是散在 _Layout.cshtml，是因為之後還會有很多欄位改成可維護的選單
/// （人員、部門、地點那類會隨組織異動的值）；屆時只要在對應的群組裡加一行，
/// 側邊欄就會自己長出來，不必再動版面。
///
/// 「共用」放的是不專屬於任何一張表單的選單——人員、部門這種六張表都會用到的。
/// </summary>
public static class MaintenanceCatalog
{
    public const string SharedKey = "Shared";

    /// <summary>依側邊欄由上而下的順序。前六個對應六張表單，最後是共用。</summary>
    public static readonly IReadOnlyList<MaintenanceGroup> Groups =
    [
        new("Software", "資訊資產清單-軟體(SW)", []),
        new("Data", "資訊資產清單-資料(DA)", []),
        new("Systems", "資訊系統/資料庫/伺服器盤點表", []),
        new("Inventory", "個人資料檔案盤點表-人為產出",
        [
            new("Categories", "附表一：個人資料類別"),
            new("Purposes", "附表二：特定目的列表"),
        ]),
        new("Transfers", "系統自動拋轉(出/入)清單-系統產出", []),
        new("Risk", "個人資料風險自評表",
        [
            new("RiskCategories", "3-1：風險分類編號"),
            new("RiskImpacts", "3-2：評估影響程度"),
            new("RiskLikelihoods", "3-3：評估發生可能性"),
            new("RiskEffectiveness", "3-4：有效性評估"),
        ]),
        new(SharedKey, "共用",
        [
            new("Departments", "部門"),
            new("OpsStaff", "資管維運"),
        ]),
    ];

    /// <summary>
    /// 目前這個控制器屬於哪一個群組，供側邊欄自動展開它所在的那一組。
    /// 不是維護頁面就回傳 null，此時七個群組都收合。
    /// </summary>
    public static string? GroupKeyOf(string? controller) =>
        string.IsNullOrEmpty(controller)
            ? null
            : Groups.FirstOrDefault(g => g.Items.Any(
                  i => string.Equals(i.Controller, controller, StringComparison.OrdinalIgnoreCase)))?.Key;
}

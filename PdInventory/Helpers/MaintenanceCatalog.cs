namespace PdInventory.Helpers;

/// <summary>
/// 一個維護項目：對應的控制器與側邊欄顯示名稱。
/// </summary>
/// <param name="Field">
/// 欄位選項的維護畫面共用同一個控制器，靠這個參數決定維護哪一個欄位
/// （作法比照「維護匯出」的 <c>?list=</c>）。其他維護項目留空。
/// </param>
public record MaintenanceItem(string Controller, string Title, string? Field = null);

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

    /// <summary>
    /// 依側邊欄由上而下的順序。前六個對應六張表單，最後是共用。
    ///
    /// 各群組除了這裡寫死的項目，還會自動接上 <see cref="OptionCatalog"/> 中屬於它的欄位
    /// ——見 <see cref="ItemsOf"/>。要多一個可維護的選項只要動 OptionCatalog，這裡不必改。
    /// </summary>
    private static readonly IReadOnlyList<MaintenanceGroup> Declared =
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
            new("Sections", "科別"),
            new("Employees", "人員"),
            new("OpsStaff", "資管維運"),
        ]),
    ];

    /// <summary>側邊欄實際要顯示的群組：宣告的項目加上該群組的可維護選項。</summary>
    public static readonly IReadOnlyList<MaintenanceGroup> Groups = Declared
        .Select(g => g with { Items = ItemsOf(g) })
        .ToList();

    private static List<MaintenanceItem> ItemsOf(MaintenanceGroup group) =>
    [
        .. group.Items,
        .. OptionCatalog.InGroup(group.Key)
            .Select(f => new MaintenanceItem(OptionsController, f.Title, f.Field)),
    ];

    /// <summary>欄位選項維護畫面的控制器名稱。</summary>
    public const string OptionsController = "FieldOptionItems";

    /// <summary>
    /// 目前這個控制器屬於哪一個群組，供側邊欄自動展開它所在的那一組。
    /// 選項維護畫面共用一個控制器，因此還要看是哪一個欄位才分得出群組。
    /// 不是維護頁面就回傳 null，此時七個群組都收合。
    /// </summary>
    public static string? GroupKeyOf(string? controller, string? field = null)
    {
        if (string.IsNullOrEmpty(controller)) return null;

        if (string.Equals(controller, OptionsController, StringComparison.OrdinalIgnoreCase))
            return OptionCatalog.Find(field)?.Group;

        return Groups.FirstOrDefault(g => g.Items.Any(
            i => i.Field is null
              && string.Equals(i.Controller, controller, StringComparison.OrdinalIgnoreCase)))?.Key;
    }
}

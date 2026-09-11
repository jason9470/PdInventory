namespace PdInventory.Helpers;

/// <summary>
/// 「使用中」明細視窗裡的一段：這些資料來自哪一張清單、點下去要去哪、每一列顯示哪幾欄。
///
/// 六張清單的識別欄位不一樣（DA 沒有資產名稱、拋轉清單多一個類別…），
/// 所以欄位標題掛在這裡、每一列只帶對應的儲存格內容，畫面不必為每張清單各寫一套表格。
/// </summary>
/// <param name="Key">清單代碼，與 <see cref="ListSource"/> 的白名單一致。</param>
/// <param name="Title">視窗裡那一段的小標題。</param>
/// <param name="Controller">檢視畫面所在的控制器；動作一律是 Details。</param>
/// <param name="Headers">每一列的欄位標題，順序與 <see cref="UsageRow.Cells"/> 對應。</param>
public sealed record UsageList(string Key, string Title, string Controller,
                               IReadOnlyList<string> Headers);

public static class UsageLists
{
    // SW、DA、盤點表三者共用同一個檢視畫面，主鍵是 DataAssets.Id（見 AssetGroup）
    public static readonly UsageList Software =
        new("Software", "資訊資產清單－軟體(SW)", "Data", ["資產編號", "資產名稱"]);

    public static readonly UsageList Data =
        new("Data", "資訊資產清單－資料(DA)", "Data", ["關連資產編號", "資料資產編號"]);

    public static readonly UsageList Systems =
        new("Systems", "資訊系統／資料庫／伺服器盤點表", "Data", ["資產編號", "資產名稱"]);

    public static readonly UsageList Inventory =
        new("Inventory", "個人資料檔案盤點表", "Inventory",
            ["資產編號", "資產名稱", "含個資之文件/檔案/表單"]);

    public static readonly UsageList Transfers =
        new("Transfers", "系統自動拋轉清單", "Transfers", ["資產編號", "資產名稱", "類別"]);

    public static readonly UsageList Risk =
        new("Risk", "個人資料風險自評表", "Risk", ["資產編號", "資產名稱", "個資文件名稱"]);

    /// <summary>視窗裡分段的先後順序。</summary>
    public static readonly IReadOnlyList<UsageList> All =
        [Software, Data, Systems, Inventory, Transfers, Risk];
}

/// <summary>
/// 明細視窗裡的一列。
/// </summary>
/// <param name="List">屬於哪一張清單，決定分在哪一段、點下去連到哪個控制器。</param>
/// <param name="Id">該清單檢視畫面的主鍵。</param>
/// <param name="Cells">要顯示的欄位內容，順序與 <see cref="UsageList.Headers"/> 對應。</param>
/// <param name="Field">
/// 命中的是哪一個欄位。不能省：同一筆 SW 可能在「使用單位」與「業務權責單位」
/// 兩個欄位都填了同一個部門，不寫出來會看起來像同一列重複了兩次。
/// </param>
public sealed record UsageRow(UsageList List, int Id, IReadOnlyList<string> Cells, string Field);

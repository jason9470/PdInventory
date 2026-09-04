using Microsoft.AspNetCore.Mvc.Rendering;
using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>
/// 值固定、不隨組織異動的欄位選項。
///
/// 與「維護資料」表（附表一、附表二、3-1~3-4）的分工：單位、人員、地點那類會隨組織
/// 變動的選項要進資料表，由管理者自行維護；這裡放的是像「是／否」「線上／下線」
/// 這種本質上不會變的，寫在程式裡即可，不值得為它們各開一張維護表。
///
/// 選項來自 docs/欄位選項化盤點.md 的逐欄統計，並已確認涵蓋資料庫現有的所有值。
///
/// 還有第三種來源：<see cref="OptionCatalog"/>——值固定但**業務端要能自己維護**的欄位
/// （位置、使用語言、自行/委外開發…），選項存在資料表，由側邊欄「維護資料」下的畫面管理。
/// </summary>
public static class FieldOptions
{
    /// <summary>
    /// 保管單位與風險擁有者由業務端指定為固定值，畫面上唯讀、不可更改。
    ///
    /// 寫死在程式裡是照業務端的要求；代價是異動（例如風險擁有者換人）要改這裡再重新部署。
    /// 若之後變成會換的資料，改成維護資料表即可，兩個常數只有這裡與 AppDbContext 兩處引用。
    /// </summary>
    public const string FixedCustodianUnit = "資訊系統開發一部";
    public const string FixedRiskOwner = "陳映玲";

    private static readonly string[] YesNo = ["是", "否"];
    private static readonly string[] YesNoNa = ["是", "否", "N/A"];
    private static readonly string[] HasNoneNa = ["有", "無", "N/A"];
    private static readonly string[] Levels = ["1", "2", "3", "4"];

    private static readonly Dictionary<string, string[]> Map = new()
    {
        // ── 資訊資產清單－軟體(SW) ───────────────────────────
        // 0902 的來源資料在「否」後面加了括號說明，屬於既有答案的細分
        [nameof(InfoSystem.SwAdIntegration)] =
            ["是", "否", "否(電子交易密碼)", "否(無AP密碼)", "否(作業系統密碼)"],
        [nameof(InfoSystem.SwLocalHa)] = YesNo,
        [nameof(InfoSystem.SwRemoteHa)] = YesNo,
        [nameof(InfoSystem.SwHasUiAuth)] = YesNo,
        [nameof(InfoSystem.SwKeepsPdTrail)] = YesNo,
        [nameof(InfoSystem.SwProvidesApi)] = YesNo,
        // 原始資料用的是「是／無處理個資」，已統一為是／否
        [nameof(InfoSystem.SwHandlesPersonalData)] = YesNo,
        [nameof(InfoSystem.SwProvidesAccountReport)] = YesNoNa,
        [nameof(InfoSystem.SwLocalBackup)] = YesNoNa,
        [nameof(InfoSystem.SwHasRecoveryPlan)] = HasNoneNa,
        [nameof(InfoSystem.SwHasDrDrill)] = HasNoneNa,

        // 「下線」目前資料裡還沒出現過，但那是資產狀態本來就有的另一個值，先留著
        [nameof(InfoSystem.SwStatus)] = ["線上", "下線"],
        [nameof(InfoSystem.SwAssetType)] = ["軟體"],
        [nameof(InfoSystem.SwSystemCategory)] = ["一般系統", "核心系統", "套裝系統"],
        [nameof(InfoSystem.SwConfidentiality)] = Levels,
        [nameof(InfoSystem.SwIntegrity)] = Levels,
        [nameof(InfoSystem.SwAvailability)] = Levels,
        [nameof(InfoSystem.SwVersionControl)] = ["TFS", "N/A"],
        [nameof(InfoSystem.SwDeployMethod)] = ["OP過版", "N/A"],
        [nameof(InfoSystem.SwCodeAccess)] = ["目錄與OP分開", "N/A"],
        [nameof(InfoSystem.SwThirdPartyComponents)] = ["詳列於第三方元件檢測平台", "無", "委外系統", "N/A"],
        [nameof(InfoSystem.SwRto)] = ["15分鐘", "30分鐘", "1小時", "2小時", "4小時", "8小時", "24小時", "N/A"],
        [nameof(InfoSystem.SwRpo)] =
            ["15分鐘", "30分鐘", "1小時", "2小時", "3小時", "4小時", "8小時", "24小時", "168小時", "不適用", "N/A"],

        // ── 資訊資產清單－資料(DA) ───────────────────────────
        [nameof(DataAsset.DaAssetType)] = ["資料"],

        // ── 個人資料檔案盤點表－人為產出 ─────────────────────
        // 這三個欄位目前的資料只出現過單一值，其餘選項當初是依欄位語意補上的，
        // 2026-09-03 已經業務端確認可以照這樣保留。
        [nameof(InventoryItem.SubjectType)] = ["客戶", "員工", "其他"],
        [nameof(InventoryItem.SpecialDataLegalBasis)] = ["Y", "N"],
        [nameof(InventoryItem.CompanyRole)] = ["資料控制者", "資料處理者"],

        [nameof(InventoryItem.TransferMethod)] = ["SFTP加密檔案", "FTP加密檔案", "電子交換"],
        [nameof(InventoryItem.Disposal)] = ["資料抹除", "永久保存", "設定排程自動清除資料"],

        // ── 拋轉清單 ─────────────────────────────────────────
        // 原始資料有「拋出/拋入」這種一格兩件事的寫法，2026-09-03 業務端確認
        // 一筆只會是其中一種，那 4 筆已拆成兩筆（編號加 -1），選項也只留這兩個
        [nameof(TransferRecord.TransferType)] = ["拋入", "拋出"],
    };

    /// <summary>這個欄位有沒有固定選項。</summary>
    public static bool Has(string propertyName) => Map.ContainsKey(propertyName);

    /// <summary>
    /// 產生下拉選單的項目。
    ///
    /// 若目前的值不在選項中（來源資料有例外，或選項後來調整過），會額外插入一個標示過的
    /// 選項並保留原值——否則畫面會顯示空白，使用者一按儲存就把原本的資料清掉了。
    /// </summary>
    public static List<SelectListItem> ItemsFor(string propertyName, string? currentValue)
    {
        var options = Map[propertyName];
        var items = options.Select(o => new SelectListItem(o, o)).ToList();

        var current = (currentValue ?? "").Trim();
        if (current.Length > 0 && !options.Contains(current))
            items.Insert(0, new SelectListItem($"{current}（目前值，不在選項中）", current));

        return items;
    }
}

using Microsoft.AspNetCore.Mvc.Rendering;
using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>
/// **邏輯上封閉**的欄位選項——是／否、有／無、Y／N 這種沒有第三個答案的，
/// 以及兩張清單各自的型錄常數（軟體／資料）。
///
/// 開放這些讓人編輯只有壞處：多一個「也許」並不會讓資料更準確。
/// 剩下的 15 個欄位就是這樣的性質，其餘的都已經移到 <see cref="OptionCatalog"/>。
///
/// 三種選項來源的分工：
///   FieldOptions（這裡）　　　邏輯封閉，寫死在程式裡。
///   OptionCatalog ＋ FieldOptionItems　值固定但業務端要能自己增刪，存資料表、有維護畫面。
///   Departments／Employees／OpsStaffs　組織資料，各有專屬的維護畫面。
///
/// 選項來自 docs/欄位選項化盤點.md 的逐欄統計，並已確認涵蓋資料庫現有的所有值。
///
/// **注意**：這裡還留著三組程式碼有依賴的選項，改動前要先看依賴：
///   機密性／完整性／可用性　AppDbContext 會把三個值相加算資產價值，必須是數字。
///   系統類別　　　　　　　　site.js 的 CORE_CATEGORY 寫死「核心系統」。
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

        // 這兩張清單就是這個類別，型錄常數而不是可選的答案
        [nameof(InfoSystem.SwAssetType)] = ["軟體"],
        [nameof(InfoSystem.SwSystemCategory)] = ["一般系統", "核心系統", "套裝系統"],
        [nameof(InfoSystem.SwConfidentiality)] = Levels,
        [nameof(InfoSystem.SwIntegrity)] = Levels,
        [nameof(InfoSystem.SwAvailability)] = Levels,

        // ── 資訊資產清單－資料(DA) ───────────────────────────
        [nameof(DataAsset.DaAssetType)] = ["資料"],

        // ── 個人資料檔案盤點表－人為產出 ─────────────────────
        // 目前的資料只出現過單一值，另一個選項當初是依欄位語意補上的，
        // 2026-09-03 已經業務端確認可以照這樣保留。
        [nameof(InventoryItem.SpecialDataLegalBasis)] = ["Y", "N"],
        [nameof(InventoryItem.CompanyRole)] = ["資料控制者", "資料處理者"],

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

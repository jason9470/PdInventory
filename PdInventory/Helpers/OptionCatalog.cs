using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>
/// 一個可維護選項的欄位：屬性名稱、畫面標題、屬於哪一張表單、單選還是多選。
/// </summary>
/// <param name="Field">屬性名稱，也是 <see cref="FieldOptionItem.FieldName"/> 存的值。</param>
/// <param name="Title">側邊欄與維護畫面的標題。</param>
/// <param name="Group">所屬表單，對應 <see cref="MaintenanceCatalog"/> 的群組代碼。</param>
/// <param name="IsMultiple">多選欄位（值以 / 分隔）。</param>
/// <param name="NumericOnly">
/// 選項必須是數字。機密性／完整性／可用性三個等級會被
/// <c>AppDbContext.RecalculateAssetValues</c> 相加算出資產價值，
/// 混進一個非數字的選項，資產價值會**安靜地停止計算**而不會報錯。
/// </param>
/// <param name="ReservedValues">
/// 程式碼直接依賴、不准刪除的選項。例如「核心系統」寫死在 site.js 的
/// <c>CORE_CATEGORY</c>，被刪掉之後資產價值 &gt; 10 的阻擋視窗就失效了。
/// </param>
public record OptionField(string Field, string Title, string Group, bool IsMultiple,
                          bool NumericOnly = false,
                          IReadOnlyList<string>? ReservedValues = null)
{
    public string ModeLabel => IsMultiple ? "多選" : "單選";

    public IReadOnlyList<string> Reserved => ReservedValues ?? [];

    public bool IsReserved(string value) =>
        Reserved.Any(v => string.Equals(v, value, StringComparison.Ordinal));

    /// <summary>新增選項時的檢查。通過回傳 null，否則回傳要顯示的原因。</summary>
    public string? RejectReason(string value)
    {
        if (IsMultiple && value.Contains(MultiValue.Separator))
            return $"多選欄位的選項不能包含「{MultiValue.Separator}」，那是分隔符號。";

        if (NumericOnly && !int.TryParse(value, out _))
            return $"「{Title}」的選項必須是數字——資產價值是把這三個等級相加算出來的，"
                 + "混進非數字的選項會讓它算不出來。";

        return null;
    }
}

/// <summary>
/// 哪些欄位的選項由業務端自己維護。
///
/// 與 <see cref="FieldOptions"/> 的分工：那裡寫死的是本質上不會變的（是／否、等級 1~4），
/// 這裡登記的是值固定、但會隨業務調整而增刪的——選項存在 <see cref="FieldOptionItem"/>，
/// 由側邊欄「維護資料」下對應的畫面管理。
///
/// 要讓一個欄位變成可維護，在這裡加一行、把畫面那一格改成吃
/// <c>Lookup.OptionsFor(...)</c>，側邊欄與維護畫面都會自己長出來。
/// </summary>
public static class OptionCatalog
{
    public static readonly IReadOnlyList<OptionField> Fields =
    [
        new(nameof(InfoSystem.SwOwnerUnit), "SW-權責單位", "Software", IsMultiple: false),
        new(nameof(InfoSystem.SwCustodianUnit), "SW-保管單位", "Software", IsMultiple: false),
        new(nameof(InfoSystem.SwRiskOwner), "SW-風險擁有者", "Software", IsMultiple: false),
        new(nameof(InfoSystem.SwLocation), "SW-位置", "Software", IsMultiple: true),
        new(nameof(InfoSystem.SwDevMode), "SW-自行/委外開發", "Software", IsMultiple: true),
        new(nameof(InfoSystem.SwMaintMode), "SW-自行/委外維護", "Software", IsMultiple: true),
        new(nameof(InfoSystem.SwLanguage), "SW-使用語言種類", "Software", IsMultiple: true),
        new(nameof(InfoSystem.SwTrailStorage), "SW-軌跡留存方式", "Software", IsMultiple: false),
        new(nameof(InfoSystem.SwStatus), "SW-資產狀態", "Software", IsMultiple: false),
        new(nameof(InfoSystem.SwAdIntegration), "SW-與AD整合", "Software", IsMultiple: false),
        new(nameof(InfoSystem.SwVersionControl), "SW-版控系統", "Software", IsMultiple: false),
        new(nameof(InfoSystem.SwDeployMethod), "SW-上版方式", "Software", IsMultiple: false),
        new(nameof(InfoSystem.SwCodeAccess), "SW-程式碼存取方式", "Software", IsMultiple: false),
        new(nameof(InfoSystem.SwThirdPartyComponents), "SW-第三方元件程式/版本", "Software", IsMultiple: false),
        new(nameof(InfoSystem.SwRto), "SW-回復時間目標(RTO)", "Software", IsMultiple: false),
        new(nameof(InfoSystem.SwRpo), "SW-回復資料目標(RPO)", "Software", IsMultiple: false),

        new(nameof(DataAsset.DaHasSensitiveData), "DA-有無機敏資料", "Data", IsMultiple: true),
        // 多選：既有資料有 15 筆是「資料庫同地備份/異地抄寫」這種組合
        new(nameof(DataAsset.DaBackupConfirm), "DA-確認-資料備份方式", "Data", IsMultiple: true),

        new(nameof(InventoryItem.SubjectType), "個資當事人類別", "Inventory", IsMultiple: false),
        new(nameof(InventoryItem.TransferMethod), "傳輸：對外傳遞方式", "Inventory", IsMultiple: false),
        new(nameof(InventoryItem.Disposal), "處置：期限屆滿後處置方式", "Inventory", IsMultiple: false),

        // ── 程式碼有依賴的四個，維護畫面會擋住會弄壞計算的操作 ────────────
        new(nameof(InfoSystem.SwSystemCategory), "SW-系統類別", "Software", IsMultiple: false,
            ReservedValues: ["核心系統"]),
        new(nameof(InfoSystem.SwConfidentiality), "SW-機密性", "Software", IsMultiple: false,
            NumericOnly: true),
        new(nameof(InfoSystem.SwIntegrity), "SW-完整性", "Software", IsMultiple: false,
            NumericOnly: true),
        new(nameof(InfoSystem.SwAvailability), "SW-可用性", "Software", IsMultiple: false,
            NumericOnly: true),
    ];

    public static OptionField? Find(string? field) =>
        string.IsNullOrEmpty(field)
            ? null
            : Fields.FirstOrDefault(f => string.Equals(f.Field, field, StringComparison.Ordinal));

    /// <summary>某一張表單底下的可維護選項，供側邊欄分組列出。</summary>
    public static IEnumerable<OptionField> InGroup(string group) =>
        Fields.Where(f => f.Group == group);
}

using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>
/// 一個可維護選項的欄位：屬性名稱、畫面標題、屬於哪一張表單、單選還是多選。
/// </summary>
/// <param name="Field">屬性名稱，也是 <see cref="FieldOptionItem.FieldName"/> 存的值。</param>
/// <param name="Title">側邊欄與維護畫面的標題。</param>
/// <param name="Group">所屬表單，對應 <see cref="MaintenanceCatalog"/> 的群組代碼。</param>
/// <param name="IsMultiple">多選欄位（值以 / 分隔）。</param>
public record OptionField(string Field, string Title, string Group, bool IsMultiple)
{
    public string ModeLabel => IsMultiple ? "多選" : "單選";
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
        new(nameof(InfoSystem.SwLocation), "SW-位置", "Software", IsMultiple: true),
        new(nameof(InfoSystem.SwDevMode), "SW-自行/委外開發", "Software", IsMultiple: true),
        new(nameof(InfoSystem.SwMaintMode), "SW-自行/委外維護", "Software", IsMultiple: true),
        new(nameof(InfoSystem.SwLanguage), "SW-使用語言種類", "Software", IsMultiple: true),
        new(nameof(InfoSystem.SwTrailStorage), "SW-軌跡留存方式", "Software", IsMultiple: false),
        new(nameof(DataAsset.DaHasSensitiveData), "DA-有無機敏資料", "Data", IsMultiple: true),
    ];

    public static OptionField? Find(string? field) =>
        string.IsNullOrEmpty(field)
            ? null
            : Fields.FirstOrDefault(f => string.Equals(f.Field, field, StringComparison.Ordinal));

    /// <summary>某一張表單底下的可維護選項，供側邊欄分組列出。</summary>
    public static IEnumerable<OptionField> InGroup(string group) =>
        Fields.Where(f => f.Group == group);
}

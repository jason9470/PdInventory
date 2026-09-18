using PdInventory.Helpers;

namespace PdInventory.Models.ViewModels;

/// <summary>
/// 多選欄位的勾選清單（_MultiCheckList.cshtml）。
///
/// 欄位本身仍然是一格文字（以 / 分隔），畫面上真正送出的是一個 hidden input；
/// 勾選只是在改那個 hidden 的值。這樣做的好處是控制器與 ViewModel 完全不必更動，
/// 之後要把別的欄位改成多選，只要在畫面換一行。
/// </summary>
/// <param name="FieldName">送出的欄位名稱，例如 <c>Software.SwUserUnit</c>。</param>
/// <param name="Value">目前存的值（未切開的原字串）。</param>
/// <param name="Options">可勾選的選項，通常來自維護資料表。</param>
/// <param name="AllowCustom">
/// 允許自行輸入清單以外的值。委外廠商就是這種情況——業務端決定廠商不進維護表，
/// 打進來的名稱也不回寫，因此人員類欄位要留一個「其他」讓人直接填。
/// </param>
/// <param name="Teams">
/// 人員類欄位專用：姓名 → 組別。給了就會在每個勾選框掛上 data-team，
/// site.js 依「SW-權責單位」選到的組隱藏其他組的人（0918）。
/// </param>
public record MultiCheckListModel(string FieldName, string? Value, IReadOnlyList<string> Options,
                                  bool AllowCustom = false,
                                  IReadOnlyDictionary<string, string>? Teams = null)
{
    /// <summary>某個選項的組別；沒給 Teams（不是人員欄位）時回傳 null。</summary>
    public string? TeamOf(string value) =>
        Teams is null ? null : Teams.GetValueOrDefault(value, "");

    /// <summary>目前已選的值，依原字串的順序。</summary>
    public List<string> Selected { get; } = MultiValue.Split(Value);

    /// <summary>
    /// 已選、但不在選項清單裡的值。照樣列出來並打勾，否則使用者一勾別的項目就把它們弄丟了。
    /// </summary>
    public List<string> Unknown => Selected.Where(v => !Options.Contains(v)).ToList();

    /// <summary>HTML id 用的名稱：欄位名稱裡的點在 CSS 選擇器與 label 的 for 都不好用。</summary>
    public string ElementId => FieldName.Replace('.', '_');
}

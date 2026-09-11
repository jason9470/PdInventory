using PdInventory.Helpers;

namespace PdInventory.Models.ViewModels;

/// <summary>
/// 「使用中」明細視窗的內容：哪一個值、被哪些資料使用。
/// 明細依來源清單分段呈現，段落順序固定（見 <see cref="UsageLists.All"/>）。
/// </summary>
/// <param name="Title">視窗標題裡顯示的那個值，例如部門名稱或選項文字。</param>
/// <param name="Rows">明細列，未分組。</param>
public sealed record UsageListViewModel(string Title, IReadOnlyList<UsageRow> Rows)
{
    public int Total => Rows.Count;

    /// <summary>依來源清單分段，空的段落不呈現。</summary>
    public IEnumerable<(UsageList List, List<UsageRow> Rows)> Sections =>
        UsageLists.All
            .Select(list => (list, Rows.Where(r => r.List.Key == list.Key).ToList()))
            .Where(pair => pair.Item2.Count > 0);
}

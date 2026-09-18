namespace PdInventory.Models.ViewModels;

/// <summary>
/// 人員單選下拉（_PersonSelect.cshtml）。選項帶著組別，畫面才能依「SW-權責單位」
/// 選到的組即時篩選（0918）。用到的欄位：SW-應用系統主管、SW-115上檢視人員、DA-115檢視人員。
/// </summary>
/// <param name="FieldName">送出的欄位名稱，例如 <c>Software.SwReviewer</c>。</param>
/// <param name="Value">目前存的值（姓名）。</param>
/// <param name="People">可選的人員，姓名與組別。</param>
public record PersonSelectModel(string FieldName, string? Value,
                                IReadOnlyList<(string Name, string Team)> People)
{
    /// <summary>HTML id 用的名稱：欄位名稱裡的點在 CSS 選擇器與 label 的 for 都不好用。</summary>
    public string ElementId => FieldName.Replace('.', '_');

    /// <summary>目前值不在清單裡（離職、或本來就是清單外的名稱）。</summary>
    public bool ValueIsUnknown => !string.IsNullOrWhiteSpace(Value)
                                  && !People.Any(p => p.Name == Value);
}

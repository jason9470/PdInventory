namespace PdInventory.Models.ViewModels;

/// <summary>
/// 階層圖：部室 → 組 → 科 → 項目。畫法共用 Views/Shared/_OrgTree.cshtml，目前有兩種內容：
///   人員 → [組織圖]：科底下列人（組長、科長排前面）
///   科別 → [負責系統圖]：科底下列負責的系統
/// 資料全部來自科別表、人員表與科別負責系統的對照，不另外維護一份。
/// </summary>
public class OrgChartViewModel
{
    /// <summary>部室層級（部室主管、部室副主管…），依科別排序。第一個畫在最上面，其餘從主幹旁接出。</summary>
    public List<OrgUnit> Offices { get; set; } = [];

    /// <summary>各組，依科別排序。</summary>
    public List<OrgTeam> Teams { get; set; } = [];

    /// <summary>沒有畫進圖裡的東西（沒指定科別的人、沒有任何科負責的系統），畫面下方列出提醒。</summary>
    public List<string> Unassigned { get; set; } = [];

    /// <summary>提醒的開頭，例如「沒有指定科別的人」。</summary>
    public string UnassignedLabel { get; set; } = "";
}

/// <summary>一個方塊。</summary>
/// <param name="Name">科別名稱。</param>
/// <param name="Tag">方塊右下角名牌上的字（組長姓名、「共 7 套」…），空字串就不畫名牌。</param>
/// <param name="Items">方塊底下列出的項目。</param>
public record OrgUnit(int Id, string Name, string Tag, List<OrgItem> Items);

/// <summary>方塊底下的一行。</summary>
/// <param name="Text">主要文字（姓名、系統名稱）。</param>
/// <param name="Code">前面的代碼（資產編號），以等寬字顯示；沒有就是 null。</param>
/// <param name="Badge">後面的小標記（科長、組長），沒有就是 null。</param>
/// <param name="Strong">是否粗體（主管）。</param>
public record OrgItem(string Text, string? Code = null, string? Badge = null, bool Strong = false);

/// <summary>一個組：組本身加上底下的科。</summary>
public record OrgTeam(OrgUnit Team, List<OrgUnit> Sections);

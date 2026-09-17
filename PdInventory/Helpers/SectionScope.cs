using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>
/// 科別能修改哪些系統。權限判斷、人員畫面、權限設定畫面、科別畫面全部走這一支，
/// 算法只有一份，畫面上看到的範圍就是實際生效的範圍。
///
///   科   → 直接對應的系統
///   組   → 同組別底下所有「科」的聯集（組長用；業務端的對照檔裡組那一列剛好等於聯集）
///   部室 → 沒有（部室主管與副主管靠管理者角色）
///
/// 資料量是二十幾個科別、五十幾筆對照，一次全部撈回來在記憶體裡算最直接。
/// </summary>
public sealed class SectionScope
{
    private readonly Dictionary<int, Section> _sections;
    private readonly Dictionary<int, List<InfoSystem>> _direct;

    private SectionScope(Dictionary<int, Section> sections, Dictionary<int, List<InfoSystem>> direct)
    {
        _sections = sections;
        _direct = direct;
    }

    public static async Task<SectionScope> LoadAsync(AppDbContext db)
    {
        var sections = await db.Sections.AsNoTracking().ToDictionaryAsync(s => s.Id);

        // 從對照表這一端 Include 系統：參考導覽是單純的 INNER JOIN。
        // 反過來從科別 Include 集合再帶出系統，EF 會翻成 SQLite 不支援的 APPLY。
        var links = await db.SectionSystems.AsNoTracking()
            .Include(x => x.InfoSystem)
            .ToListAsync();

        var direct = links
            .GroupBy(x => x.SectionId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.InfoSystem!).ToList());

        return new SectionScope(sections, direct);
    }

    /// <summary>這個科別能修改的系統，依資產編號排序。找不到科別或沒有科別時回傳空清單。</summary>
    public List<InfoSystem> SystemsOf(int? sectionId)
    {
        if (sectionId is null || !_sections.TryGetValue(sectionId.Value, out var section))
            return [];

        IEnumerable<InfoSystem> systems = section.Kind switch
        {
            SectionKind.Section => _direct.GetValueOrDefault(section.Id) ?? [],
            SectionKind.Team => _sections.Values
                .Where(s => s.Kind == SectionKind.Section && s.TeamName == section.TeamName)
                .SelectMany(s => _direct.GetValueOrDefault(s.Id) ?? []),
            _ => [],
        };

        // SW-118 同時屬於兩個科，組長那邊聯集時會出現兩次
        return systems.DistinctBy(s => s.Id).OrderBy(s => s.SystemCode).ToList();
    }

    /// <summary>
    /// 依「科」分段的系統清單，給組長看的畫面用：每一科各負責哪幾套。
    /// 科本身就只有自己一段；部室與沒有科別的人是空的。
    /// </summary>
    public List<(Section Section, List<InfoSystem> Systems)> BreakdownOf(int? sectionId)
    {
        if (sectionId is null || !_sections.TryGetValue(sectionId.Value, out var section))
            return [];

        var parts = section.Kind switch
        {
            SectionKind.Section => [section],
            SectionKind.Team => _sections.Values
                .Where(s => s.Kind == SectionKind.Section && s.TeamName == section.TeamName)
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
                .ToList(),
            _ => new List<Section>(),
        };

        return parts
            .Select(s => (s, (_direct.GetValueOrDefault(s.Id) ?? []).OrderBy(x => x.SystemCode).ToList()))
            .ToList();
    }

    /// <summary>這個科別能修改的資產編號。權限判斷用。</summary>
    public HashSet<string> CodesOf(int? sectionId) =>
        SystemsOf(sectionId).Select(s => s.SystemCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
}

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>一個可匯出的欄位：屬性名稱、顯示標題、是否納入匯出。</summary>
public record ExportColumnInfo(string PropertyName, string Title, bool Included);

/// <summary>
/// 六張清單各自可匯出的欄位清單，以及與 ExportColumns 設定合併後的實際順序。
///
/// 欄位群組以屬性名稱前綴推導而非寫死清單，模型加減欄位時會自動跟上：
///   Software = 基本欄位 + Sw*、Data = 基本欄位 + Da*、Systems = 其餘（Sheet3）欄位。
/// </summary>
public static class ExportCatalog
{
    /// <summary>SW / DA / 系統盤點三張清單共用 InfoSystems，這三個欄位是它們的共同識別欄位。</summary>


    /// <summary>清單代碼 → (實體型別, 畫面名稱, 匯出檔名)。</summary>
    public static readonly IReadOnlyDictionary<string, (Type Entity, string Title)> Lists =
        new Dictionary<string, (Type, string)>
        {
            ["Software"]  = (typeof(InfoSystem),     "資訊資產清單-軟體(SW)"),
            ["Data"]      = (typeof(DataAsset),      "資訊資產清單-資料(DA)"),
            ["Systems"]   = (typeof(SystemInventory), "資訊系統、資料庫與檔案伺服器盤點表"),
            ["Inventory"] = (typeof(InventoryItem),  "個人資料檔案盤點表-人為產出"),
            ["Transfers"] = (typeof(TransferRecord), "系統自動拋轉(出入)清單-系統產出"),
            ["Risk"]      = (typeof(InventoryItem),  "個人資料風險自評表"),
        };

    public static bool IsKnown(string? listKey) =>
        !string.IsNullOrEmpty(listKey) && Lists.ContainsKey(listKey);

    /// <summary>該清單可匯出的屬性，依模型宣告順序。</summary>
    public static List<PropertyInfo> PropertiesOf(string listKey)
    {
        var entity = Lists[listKey].Entity;
        var all = entity.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanRead && p.Name != "Id")
                        .OrderBy(p => p.MetadataToken)
                        .ToList();

        // SW／DA／系統盤點已各自獨立成表，一張清單就是一個實體的全部欄位，
        // 不必再依欄位名稱前綴分組。
        return all;
    }

    public static string TitleOf(PropertyInfo p) =>
        p.GetCustomAttribute<DisplayAttribute>()?.Name ?? p.Name;

    /// <summary>
    /// 合併模型欄位與已儲存的設定，得出實際的匯出順序。
    /// 以模型為準：設定裡已不存在的屬性自動忽略，模型新增的屬性補在最後，
    /// 因此欄位增刪不會讓既有設定失效。
    /// </summary>
    public static async Task<List<ExportColumnInfo>> ResolveAsync(AppDbContext db, string listKey)
    {
        var props = PropertiesOf(listKey);
        var saved = await db.ExportColumns
            .Where(c => c.ListKey == listKey)
            .ToDictionaryAsync(c => c.PropertyName);

        return props
            .Select((p, index) => new
            {
                Prop = p,
                Index = index,
                Config = saved.GetValueOrDefault(p.Name),
            })
            .OrderBy(x => x.Config?.SortOrder ?? int.MaxValue)
            .ThenBy(x => x.Index)
            .Select(x => new ExportColumnInfo(x.Prop.Name, TitleOf(x.Prop), x.Config?.Included ?? true))
            .ToList();
    }

    /// <summary>依畫面送回的順序覆寫設定。</summary>
    public static async Task SaveAsync(AppDbContext db, string listKey, string[] propertyNames, string[] included)
    {
        var valid = PropertiesOf(listKey).Select(p => p.Name).ToHashSet();
        var includedSet = included.ToHashSet();

        var existing = await db.ExportColumns.Where(c => c.ListKey == listKey).ToListAsync();
        db.ExportColumns.RemoveRange(existing);

        var order = 0;
        foreach (var name in propertyNames.Where(valid.Contains))
        {
            db.ExportColumns.Add(new ExportColumn
            {
                ListKey = listKey,
                PropertyName = name,
                SortOrder = order++,
                Included = includedSet.Contains(name),
            });
        }

        await db.SaveChangesAsync();
    }
}

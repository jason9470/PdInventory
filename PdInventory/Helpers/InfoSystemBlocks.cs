using System.Reflection;
using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>
/// InfoSystem 的三個欄位群組。SW／DA／系統盤點三張清單共用同一列資料，
/// 各自的編輯區塊只能寫入自己那一組欄位。
///
/// 群組以屬性名稱前綴推導而非寫死清單：模型加減欄位會自動跟上，
/// 不會再發生「加了欄位但忘了改 Apply 方法，導致存不進去且不報錯」的情形。
/// </summary>
public static class InfoSystemBlocks
{
    public const string Sw = "Sw";
    public const string Da = "Da";
    public const string Sheet3 = "Sheet3";

    /// <summary>
    /// 不接受畫面寫入的欄位：主鍵、系統軌跡、軟刪除註記與並行權杖。
    /// 這些一律由 AppDbContext 或 Delete 動作自行維護。
    /// </summary>
    private static readonly HashSet<string> NotWritable =
    [
        nameof(InfoSystem.Id),
        nameof(InfoSystem.CreatedBy), nameof(InfoSystem.CreatedAt),
        nameof(InfoSystem.UpdatedBy), nameof(InfoSystem.UpdatedAt),
        nameof(InfoSystem.RowVersion),
        nameof(InfoSystem.IsDeleted), nameof(InfoSystem.DeletedAt), nameof(InfoSystem.DeletedBy),
    ];

    private static readonly Dictionary<string, PropertyInfo[]> Groups = Build();

    private static Dictionary<string, PropertyInfo[]> Build()
    {
        var all = typeof(InfoSystem)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !NotWritable.Contains(p.Name))
            .ToArray();

        return new Dictionary<string, PropertyInfo[]>
        {
            [Sw] = all.Where(p => p.Name.StartsWith(Sw)).ToArray(),
            [Da] = all.Where(p => p.Name.StartsWith(Da)).ToArray(),
            // 系統盤點＝其餘欄位，含編號／資產編號／資產名稱這三個共用識別欄位。
            // 共用欄位只由這一組維護，SW 與 DA 區塊不寫入，避免以載入當下的舊值互相覆蓋。
            [Sheet3] = all.Where(p => !p.Name.StartsWith(Sw) && !p.Name.StartsWith(Da)).ToArray(),
        };
    }

    public static IReadOnlyList<PropertyInfo> Of(string block) => Groups[block];

    /// <summary>把 source 的指定欄位群組複製到 target，其餘欄位保持不動。</summary>
    public static void Copy(InfoSystem target, InfoSystem source, string block)
    {
        foreach (var property in Groups[block])
            property.SetValue(target, property.GetValue(source));
    }
}

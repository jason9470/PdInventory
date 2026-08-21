using System.Reflection;
using PdInventory.Models;
using PdInventory.Models.ViewModels;

namespace PdInventory.Helpers;

/// <summary>
/// InfoSystem 的三個欄位群組。SW／DA／系統盤點三張清單共用同一列資料，
/// 各自的編輯區塊只能寫入自己那一組欄位。
///
/// 群組以屬性名稱前綴推導而非寫死清單：模型加減欄位會自動跟上，
/// 不會再發生「加了欄位但忘了改 Apply 方法，導致存不進去且不報錯」的情形。
///
/// 畫面送進來的是各區塊的 ViewModel（見 Models/ViewModels/InfoSystemEditViewModels.cs），
/// 實體與 ViewModel 之間以同名屬性對應，複製的範圍一律由這裡的分組決定。
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

    /// <summary>每個區塊對應的 ViewModel 型別，供啟動時的一致性檢查使用。</summary>
    private static readonly Dictionary<string, Type> ViewModelTypes = new()
    {
        [Sw] = typeof(SoftwareEditViewModel),
        [Da] = typeof(DataEditViewModel),
        [Sheet3] = typeof(SystemEditViewModel),
    };

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

    /// <summary>把畫面送回的 ViewModel 寫入實體，只碰該區塊的欄位。</summary>
    public static void CopyToEntity(InfoSystem target, object source, string block) =>
        Transfer(source, target, block);

    /// <summary>把實體的值填進 ViewModel，供編輯畫面呈現。</summary>
    public static void FillViewModel(object target, InfoSystem source, string block) =>
        Transfer(source, target, block);

    /// <summary>
    /// 以屬性名稱在兩個物件間搬移某個區塊的欄位。兩邊的型別不必相同，
    /// 但同名屬性一定存在——啟動時的一致性檢查已經確保過了。
    /// </summary>
    private static void Transfer(object source, object target, string block)
    {
        var sourceType = source.GetType();
        var targetType = target.GetType();

        foreach (var property in Groups[block])
        {
            var from = sourceType.GetProperty(property.Name);
            var to = targetType.GetProperty(property.Name);
            if (from is null || to is null || !to.CanWrite) continue;
            to.SetValue(target, from.GetValue(source));
        }
    }

    /// <summary>
    /// 由實體組出統一編輯畫面的模型：三個區塊各自填好，並帶上共用的主鍵與並行權杖。
    /// </summary>
    public static InfoSystemEditViewModel ToEditViewModel(InfoSystem entity)
    {
        var model = new InfoSystemEditViewModel { Asset = entity };

        FillViewModel(model.Software, entity, Sw);
        FillViewModel(model.Data, entity, Da);
        FillViewModel(model.Sheet3, entity, Sheet3);

        foreach (var block in new IInfoSystemBlockViewModel[] { model.Software, model.Data, model.Sheet3 })
        {
            block.Id = entity.Id;
            block.RowVersion = entity.RowVersion;
        }

        return model;
    }

    /// <summary>
    /// 檢查三個 ViewModel 是否涵蓋了各自區塊的所有欄位。
    ///
    /// ViewModel 是手寫的屬性清單，實體加了新欄位卻忘了同步時，欄位會在畫面上消失、
    /// 存檔時安靜地保持舊值——那種錯誤很難從畫面看出來。改成在啟動時就擲出例外，
    /// 讓它變成一眼可見的失敗。由 Program.cs 在建好服務後呼叫。
    /// </summary>
    public static void AssertViewModelsCoverAllFields()
    {
        var missing = new List<string>();

        foreach (var (block, viewModelType) in ViewModelTypes)
        {
            var declared = viewModelType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet();

            missing.AddRange(Groups[block]
                .Where(property => !declared.Contains(property.Name))
                .Select(property => $"{viewModelType.Name} 缺少 {property.Name}"));
        }

        if (missing.Count > 0)
            throw new InvalidOperationException(
                "InfoSystem 的欄位與編輯畫面的 ViewModel 不一致：" + Environment.NewLine
                + string.Join(Environment.NewLine, missing));
    }
}

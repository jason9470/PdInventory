using System.Reflection;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Models.ViewModels;

namespace PdInventory.Helpers;

/// <summary>
/// SW／DA／系統盤點三個編輯區塊與實體之間的搬移。
///
/// 這三者原本是 InfoSystems 同一列的三組欄位，現已各自獨立成表
/// （InfoSystem／DataAsset／SystemInventory），因此這裡不再需要「依前綴分組」，
/// 改為單純依屬性名稱在 ViewModel 與實體之間對應。
/// </summary>
public static class InfoSystemBlocks
{
    /// <summary>
    /// 不接受畫面寫入的欄位：主鍵、系統軌跡、軟刪除註記與並行權杖。
    /// 這些一律由 AppDbContext 或 Delete 動作自行維護。
    /// </summary>
    private static readonly HashSet<string> NotWritable =
    [
        "Id", "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt",
        "RowVersion", "IsDeleted", "DeletedAt", "DeletedBy",
    ];

    /// <summary>
    /// 把來源物件的同名屬性複製到目標。以「來源有什麼就搬什麼」為準，
    /// 因此 ViewModel 沒有的欄位（例如實體的軌跡）不會被動到。
    /// </summary>
    public static void Copy(object target, object source)
    {
        var targetType = target.GetType();

        foreach (var from in source.GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanRead && !NotWritable.Contains(p.Name)))
        {
            var to = targetType.GetProperty(from.Name);
            if (to is null || !to.CanWrite || to.PropertyType != from.PropertyType) continue;
            to.SetValue(target, from.GetValue(source));
        }
    }

    /// <summary>
    /// 組出統一編輯畫面的模型。DA 與盤點表現在是獨立的資料列，可能不存在
    /// （新資產還沒填、或這個 SW 根本沒有對應的資料資產），此時該區塊呈現空白。
    /// </summary>
    public static InfoSystemEditViewModel ToEditViewModel(
        InfoSystem system, DataAsset? dataAsset, SystemInventory? inventory)
    {
        var model = new InfoSystemEditViewModel
        {
            Asset = system,
            DataAssetId = dataAsset?.Id,
            InventoryId = inventory?.Id,
        };

        Copy(model.Software, system);
        model.Software.Id = system.Id;
        model.Software.RowVersion = system.RowVersion;

        if (dataAsset is not null)
        {
            Copy(model.Data, dataAsset);
            model.Data.Id = dataAsset.Id;
            model.Data.RowVersion = dataAsset.RowVersion;
        }
        else
        {
            // 還沒有 DA 資料時，先把關連編號帶好，存檔就會建出對應的那一列
            model.Data.SystemCode = system.SystemCode;
        }

        if (inventory is not null)
        {
            Copy(model.Sheet3, inventory);
            model.Sheet3.Id = inventory.Id;
            model.Sheet3.RowVersion = inventory.RowVersion;
        }
        else
        {
            model.Sheet3.SystemCode = system.SystemCode;
        }

        return model;
    }

    /// <summary>
    /// 檢查三個 ViewModel 是否涵蓋了各自實體的所有可寫欄位。
    ///
    /// ViewModel 是手寫的屬性清單，實體加了新欄位卻忘了同步時，欄位會在畫面上消失、
    /// 存檔時安靜地保持舊值——那種錯誤很難從畫面看出來。改成在啟動時就擲出例外，
    /// 讓它變成一眼可見的失敗。由 Program.cs 在建好服務後呼叫。
    /// </summary>
    public static void AssertViewModelsCoverAllFields()
    {
        var pairs = new (Type Entity, Type ViewModel)[]
        {
            (typeof(InfoSystem), typeof(SoftwareEditViewModel)),
            (typeof(DataAsset), typeof(DataEditViewModel)),
            (typeof(SystemInventory), typeof(SystemEditViewModel)),
        };

        var missing = new List<string>();

        foreach (var (entity, viewModel) in pairs)
        {
            var declared = viewModel
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet();

            missing.AddRange(entity
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.PropertyType == typeof(string)
                            && !NotWritable.Contains(p.Name) && !declared.Contains(p.Name))
                .Select(p => $"{viewModel.Name} 缺少 {entity.Name}.{p.Name}"));
        }

        if (missing.Count > 0)
            throw new InvalidOperationException(
                "實體的欄位與編輯畫面的 ViewModel 不一致：" + Environment.NewLine
                + string.Join(Environment.NewLine, missing));
    }
}

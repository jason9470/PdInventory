using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>
/// 一筆資訊資產的三個面：SW（InfoSystem）、DA（DataAsset）、盤點表（SystemInventory）。
///
/// 0910 起統一新增／編輯／檢視畫面改以 <b>DA 為主鍵</b>（<c>/Data/Edit/{DataAssets.Id}</c>），
/// 因為「有 DA 不一定有 SW」——各組的 PC／NB 那類資料資產沒有對應的軟體資產，
/// 以 SW 當主鍵時它們根本沒有網址可以開。反過來每個 SW 一定配一列 DA
/// （原本沒有的六筆在 0910 補建了空白列），所以 DA 是唯一能涵蓋全部資料的入口。
///
/// SW 與盤點表都靠 <see cref="DataAsset.SystemCode"/> 找回來；DA 的 SystemCode 空白時
/// 就是「沒有關連 SW」，那兩個區塊在畫面上直接不顯示。
/// </summary>
/// <param name="Data">主鍵所在的那一列，一定存在。</param>
/// <param name="System">關連的軟體資產；沒有關連 SW 時為 null。</param>
/// <param name="Inventory">關連的盤點表；55 套系統裡只有 26 套有，其餘為 null。</param>
public sealed record AssetGroup(DataAsset Data, InfoSystem? System, SystemInventory? Inventory)
{
    /// <summary>
    /// 關連的資產編號；沒有關連 SW 時為空字串。
    ///
    /// 這是<b>載入當下的快照</b>，不是 <c>Data.SystemCode</c> 的即時投影：存檔流程會把
    /// 畫面送來的值複製進 Data，而 DA 區塊根本沒有這個欄位、送來的是空字串。
    /// 若這裡跟著變動，「存回原本的關連編號」就會變成「存回剛剛被清空的值」，
    /// 一存檔就把關聯弄丟——實測踩過一次。
    /// </summary>
    public string SystemCode { get; } = Data.SystemCode;

    /// <summary>有沒有關連的軟體資產。決定畫面上要不要顯示 SW 與盤點表兩個區塊。</summary>
    public bool HasSoftware => System is not null;
}

public static class AssetGroups
{
    /// <summary>依 DataAssets 主鍵載入三個面。找不到那一列時回傳 null。</summary>
    public static async Task<AssetGroup?> LoadAsync(AppDbContext db, int dataAssetId)
    {
        var data = await db.DataAssets.FindAsync(dataAssetId);
        if (data is null) return null;

        // 沒有關連編號就不必再查：查也只會撈到別人的空字串
        if (string.IsNullOrWhiteSpace(data.SystemCode)) return new AssetGroup(data, null, null);

        return new AssetGroup(
            data,
            await db.InfoSystems.FirstOrDefaultAsync(s => s.SystemCode == data.SystemCode),
            await db.SystemInventories.FirstOrDefaultAsync(i => i.SystemCode == data.SystemCode));
    }

    /// <summary>
    /// 依資產編號找出對應的 DataAssets 主鍵，供 SW 清單與盤點表清單的按鈕連到統一畫面。
    ///
    /// 正常情況下每個 SW 都找得到（每個 SW 配一列 DA 是 0910 起的固定關係，
    /// 資料庫有唯一索引把關）；真的找不到時字典裡就沒有那個 key，
    /// 畫面會少一顆按鈕，而不是連到一個開不起來的網址。
    /// </summary>
    public static async Task<Dictionary<string, int>> DataAssetIdsAsync(
        AppDbContext db, IEnumerable<string> systemCodes)
    {
        var codes = systemCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();

        return await db.DataAssets
            .Where(d => codes.Contains(d.SystemCode))
            .ToDictionaryAsync(d => d.SystemCode, d => d.Id);
    }

    /// <summary>
    /// 整組軟刪除：SW、DA、盤點表三列一起加註記。
    ///
    /// 業務端 0910 決定的規則——三張清單是同一筆資產的三個面，從 SW 清單或 DA 清單
    /// 刪掉它就是整筆不要了。盤點表清單的刪除不走這裡：那只是「這套系統不需要盤點表」，
    /// 29 套系統本來就沒有那一列。
    /// </summary>
    public static void SoftDelete(AssetGroup group, string deletedBy)
    {
        var now = DateTime.Now;

        foreach (var row in new ISoftDeletable?[] { group.Data, group.System, group.Inventory })
        {
            if (row is null) continue;
            row.IsDeleted = true;
            row.DeletedAt = now;
            row.DeletedBy = deletedBy;
        }
    }
}

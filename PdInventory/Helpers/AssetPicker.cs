using Microsoft.EntityFrameworkCore;
using PdInventory.Data;

namespace PdInventory.Helpers;

/// <summary>
/// 個資盤點表、風險自評表與拋轉清單都以資產編號指回 InfoSystems，但資料庫層沒有外鍵，
/// 過去是自由輸入，打錯字就會讓關聯斷掉且看不出來。改成從這裡取得的清單挑選。
///
/// 名稱一律由伺服器端依編號查出後寫入，不採信畫面送回來的值——
/// 否則使用者改了編號卻沒改名稱時，兩者會對不起來。
/// </summary>
public static class AssetPicker
{
    /// <summary>下拉選單的一個選項。</summary>
    public sealed record Option(string Code, string Name)
    {
        /// <summary>選單上顯示的文字，例如「SW-021　電子帳單系統」。</summary>
        public string Label => string.IsNullOrWhiteSpace(Name) ? Code : $"{Code}　{Name}";
    }

    /// <summary>可挑選的資產。已軟刪除的資產由全域查詢篩選排除，不會出現在選單中。</summary>
    public static async Task<List<Option>> LoadAsync(AppDbContext db) =>
        await db.InfoSystems
            .OrderBy(s => s.SystemCode)
            .Select(s => new Option(s.SystemCode, s.SystemName))
            .ToListAsync();

    /// <summary>
    /// 依資產編號查出資產名稱。查不到（編號空白或該資產已被刪除）時回傳 null，
    /// 由呼叫端決定要不要保留原本的名稱。
    /// </summary>
    public static async Task<string?> ResolveNameAsync(AppDbContext db, string? systemCode)
    {
        if (string.IsNullOrWhiteSpace(systemCode)) return null;

        return await db.InfoSystems
            .Where(s => s.SystemCode == systemCode)
            .Select(s => s.SystemName)
            .FirstOrDefaultAsync();
    }
}

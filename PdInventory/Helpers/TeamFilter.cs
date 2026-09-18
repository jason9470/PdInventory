using Microsoft.EntityFrameworkCore;
using PdInventory.Data;

namespace PdInventory.Helpers;

/// <summary>
/// 六張清單的「組別」篩選（0918）。組別就是資訊資產的 <c>SW-權責單位</c>：
/// SW 清單直接比那一欄，其餘五張清單以 <c>SystemCode</c> 對回資訊資產。
///
/// 沒有 SystemCode、或對不到資訊資產的資料（各組的 PC／NB）選了組別就不會出現——
/// 它們不屬於任何一組，硬要歸給某一組只會讓那一組的人以為那是自己的資料。
/// </summary>
public static class TeamFilter
{
    /// <summary>某個組別底下的資產編號。組別空白時回傳 null，代表不篩。</summary>
    public static async Task<HashSet<string>?> CodesOfAsync(AppDbContext db, string? team)
    {
        if (string.IsNullOrWhiteSpace(team)) return null;

        var codes = await db.InfoSystems
            .Where(s => s.SwOwnerUnit == team && s.SystemCode != "")
            .Select(s => s.SystemCode)
            .ToListAsync();

        return codes.ToHashSet();
    }
}

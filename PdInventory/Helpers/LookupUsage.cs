using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>
/// 「這筆維護資料還被幾筆資料使用」的統計，維護畫面共用。
///
/// 為什麼每張維護表都要有：各表單存的是**文字**而不是外鍵，資料庫不會擋刪除。
/// 刪掉一個還在用的值，引用它的那些資料就變成查不到來源的孤兒值，
/// 而且畫面上不會有任何警告——這正是風險自評那四張維護表原本的狀況。
///
/// 多選欄位（以 / 分隔）不能用等號比對，一律走 <see cref="MultiValue.Contains"/>。
/// </summary>
public static class LookupUsage
{
    /// <summary>部門：對照 SW 三個欄位與拋轉清單的負責內部單位。</summary>
    public static async Task<Dictionary<string, int>> DepartmentsAsync(AppDbContext db)
    {
        var systems = await db.InfoSystems
            .Select(s => new { s.SwBusinessOwnerUnit, s.SwUserAccountGrant, s.SwUserUnit })
            .ToListAsync();
        var transfers = await db.TransferRecords.Select(t => t.InternalUnit).ToListAsync();
        var names = await db.Departments.Select(d => d.Name).ToListAsync();

        return names.ToDictionary(
            name => name,
            name => systems.Count(s => MultiValue.Contains(s.SwBusinessOwnerUnit, name)
                                    || MultiValue.Contains(s.SwUserAccountGrant, name)
                                    || MultiValue.Contains(s.SwUserUnit, name))
                  + transfers.Count(t => MultiValue.Contains(t, name)));
    }

    /// <summary>資管維運：對照 SW 的維運人員與程式換版人員。</summary>
    public static async Task<Dictionary<string, int>> OpsStaffAsync(AppDbContext db)
    {
        var systems = await db.InfoSystems
            .Select(s => new { s.SwOperator, s.SwDeployer })
            .ToListAsync();
        var names = await db.OpsStaffs.Select(o => o.Name).ToListAsync();

        return names.ToDictionary(
            name => name,
            name => systems.Count(s => MultiValue.Contains(s.SwOperator, name)
                                    || MultiValue.Contains(s.SwDeployer, name)));
    }

    /// <summary>人員：對照 SW 五個欄位與 DA 的檢視人員。同一筆多欄都是他只算一次。</summary>
    public static async Task<Dictionary<string, int>> EmployeesAsync(AppDbContext db)
    {
        var systems = await db.InfoSystems
            .Select(s => new { s.SwAppManager, s.SwAppMaintainer, s.SwAppMaintainerDeputy,
                               s.SwDeveloper, s.SwReviewer })
            .ToListAsync();
        var dataAssets = await db.DataAssets.Select(d => d.DaReviewer).ToListAsync();
        var names = await db.Employees.Select(e => e.Name).ToListAsync();

        return names.ToDictionary(
            name => name,
            name => systems.Count(s => MultiValue.Contains(s.SwAppManager, name)
                                    || MultiValue.Contains(s.SwAppMaintainer, name)
                                    || MultiValue.Contains(s.SwAppMaintainerDeputy, name)
                                    || MultiValue.Contains(s.SwDeveloper, name)
                                    || MultiValue.Contains(s.SwReviewer, name))
                  + dataAssets.Count(d => MultiValue.Contains(d, name)));
    }

    /// <summary>3-1 風險分類：風險自評存的是 Code。</summary>
    public static async Task<Dictionary<string, int>> RiskCategoriesAsync(AppDbContext db)
    {
        var used = await db.InventoryItems.Select(i => i.RiskCategoryCode).ToListAsync();
        var codes = await db.RiskCategories.Select(r => r.Code).ToListAsync();

        return codes.ToDictionary(code => code, code => used.Count(v => v == code));
    }

    /// <summary>
    /// 3-2~3-4 的等級。畫面上選的是「2：中等」這種字串，但既有資料也有只存
    /// 「2」的（有效性評估那一欄就是），兩種寫法都要算成使用中，
    /// 否則會誤判成沒人用而放行刪除。
    /// </summary>
    public static async Task<Dictionary<int, int>> RiskLevelsAsync<T>(
        AppDbContext db,
        Func<InventoryItem, string> selector,
        IEnumerable<T> levels,
        Func<T, int> levelOf,
        Func<T, string> labelOf)
    {
        var used = (await db.InventoryItems.ToListAsync()).Select(selector).ToList();

        return levels.ToDictionary(
            levelOf,
            level => used.Count(v => v == labelOf(level)
                                  || v == levelOf(level).ToString()));
    }
}

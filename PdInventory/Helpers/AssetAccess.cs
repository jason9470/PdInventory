using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>
/// 單筆資料的修改權限。
///
/// 為什麼不做成 [Authorize(Policy = ...)]：授權在動作執行前就判斷完了，那時候還沒有
/// 讀出「使用者按的是哪一筆」，Policy 看不到那一列的 SystemCode。因此權限是兩層：
/// 控制器層由 Policy 擋掉整個畫面，單筆層由這個服務擋。
///
/// 授權單位是 SW 編號（InfoSystem）。個資盤點與拋轉清單的每一列都帶著 SystemCode
/// 指回資訊資產，所以六張清單共用同一份授權。
/// </summary>
public interface IAssetAccess
{
    /// <summary>是否可以新增資料。資產負責人只有 RUD，沒有 C。</summary>
    bool CanCreate { get; }

    /// <summary>是否可以修改／刪除這個資產編號底下的資料。</summary>
    Task<bool> CanModifyAsync(string? systemCode);

    /// <summary>目前使用者名下的資產編號。主管與管理者不適用（他們不看這份清單）。</summary>
    Task<IReadOnlyCollection<string>> OwnedCodesAsync();
}

/// <inheritdoc />
public sealed class AssetAccess : IAssetAccess
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    /// <summary>同一個 request 內會被清單的每一列問一次，只查一次資料庫就好。</summary>
    private HashSet<string>? _ownedCodes;

    public AssetAccess(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public bool CanCreate => _currentUser.IsManagerOrAbove;

    public async Task<bool> CanModifyAsync(string? systemCode)
    {
        if (!_currentUser.IsAuthenticated) return false;
        if (_currentUser.IsManagerOrAbove) return true;

        // 資產編號空白或對不到任何資產時無人可認領，只有主管以上能改
        if (string.IsNullOrWhiteSpace(systemCode)) return false;

        var owned = await LoadOwnedCodesAsync();
        return owned.Contains(systemCode.Trim());
    }

    public async Task<IReadOnlyCollection<string>> OwnedCodesAsync() => await LoadOwnedCodesAsync();

    private async Task<HashSet<string>> LoadOwnedCodesAsync()
    {
        if (_ownedCodes is not null) return _ownedCodes;

        var empNo = _currentUser.EmpNo;
        if (string.IsNullOrWhiteSpace(empNo))
            return _ownedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var codes = await _db.AssetOwners
            .Where(a => a.User!.EmpNo == empNo)
            .Select(a => a.InfoSystem!.SystemCode)
            .ToListAsync();

        return _ownedCodes = new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// 授權原則名稱。字串只在這裡定義一次，Program.cs 註冊與控制器掛屬性共用，
/// 打錯字會變成編譯錯誤而不是預設放行。
/// </summary>
public static class Policies
{
    /// <summary>管理者專用：維護資料、維護匯出、權限設定。</summary>
    public const string Admin = "Admin";

    /// <summary>主管以上：六張主要清單的新增，以及不限資產的修改／刪除。</summary>
    public const string ManageAssets = "ManageAssets";

    /// <summary>已登入即可進入的清單與檢視畫面；能不能改單筆再由 IAssetAccess 判斷。</summary>
    public const string ViewAssets = "ViewAssets";
}

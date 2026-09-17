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
/// 授權單位是資產編號（SW-xxx）。個資盤點、拋轉清單與風險自評的每一列都帶著
/// SystemCode 指回資訊資產，DA 與盤點表也是，所以六張清單共用同一份判斷。
///
/// 0917 起能修改哪些系統由人員表的科別決定（見 <see cref="SectionScope"/>），
/// 不再看角色也不再看逐筆授權：
///
///   動作            管理者    其他所有人（含主管）
///   檢視            全部      全部
///   修改            全部      自己科別負責的系統（組長＝全組）
///   新增            ✔         ✘（業務端表示日後可能開放給主管）
///   刪除            ✔         ✘
/// </summary>
public interface IAssetAccess
{
    /// <summary>是否可以新增資料。與 Program.cs 的 <see cref="Policies.CreateAssets"/> 一致。</summary>
    bool CanCreate { get; }

    /// <summary>是否可以刪除資料。刪除 SW 或 DA 會連帶刪掉整筆資產，只開放給管理者。</summary>
    bool CanDelete { get; }

    /// <summary>是否可以修改這個資產編號底下的資料。</summary>
    Task<bool> CanModifyAsync(string? systemCode);

    /// <summary>目前使用者能修改的資產編號。管理者不適用（他全部都能改，不看這份清單）。</summary>
    Task<IReadOnlyCollection<string>> EditableCodesAsync();
}

/// <inheritdoc />
public sealed class AssetAccess : IAssetAccess
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    /// <summary>同一個 request 內會被清單的每一列問一次，只查一次資料庫就好。</summary>
    private HashSet<string>? _editableCodes;

    public AssetAccess(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public bool CanCreate => _currentUser.IsAdmin;

    public bool CanDelete => _currentUser.IsAdmin;

    public async Task<bool> CanModifyAsync(string? systemCode)
    {
        if (!_currentUser.IsAuthenticated) return false;
        if (_currentUser.IsAdmin) return true;

        // 資產編號空白（沒有關連 SW 的資料資產）對不到任何科，只有管理者能改
        if (string.IsNullOrWhiteSpace(systemCode)) return false;

        var editable = await LoadEditableCodesAsync();
        return editable.Contains(systemCode.Trim());
    }

    public async Task<IReadOnlyCollection<string>> EditableCodesAsync() => await LoadEditableCodesAsync();

    private async Task<HashSet<string>> LoadEditableCodesAsync()
    {
        if (_editableCodes is not null) return _editableCodes;

        var empNo = _currentUser.EmpNo;
        if (string.IsNullOrWhiteSpace(empNo))
            return _editableCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 人員表與帳號以員工編號相認；人員表查不到或沒有科別，就一套都不能改
        var sectionId = await _db.Employees
            .Where(e => e.EmpNo == empNo)
            .Select(e => e.SectionId)
            .FirstOrDefaultAsync();

        var scope = await SectionScope.LoadAsync(_db);
        return _editableCodes = scope.CodesOf(sectionId);
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

    /// <summary>
    /// 六張主要清單的新增。0917 起只開放給管理者；業務端表示日後可能開放給主管，
    /// 屆時改 Program.cs 的定義與 <see cref="AssetAccess.CanCreate"/> 兩處即可。
    /// </summary>
    public const string CreateAssets = "CreateAssets";

    /// <summary>已登入即可進入的清單與檢視畫面；能不能改單筆再由 IAssetAccess 判斷。</summary>
    public const string ViewAssets = "ViewAssets";
}

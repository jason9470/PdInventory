using System.Security.Claims;
using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>
/// 目前操作者。身分來自登入時發出的驗證 Cookie（見 Controllers/AccountController.cs），
/// 軌跡、軟刪除註記與資產授權判斷都從這裡取得「是誰」。
/// </summary>
public interface ICurrentUser
{
    /// <summary>寫入軌跡欄位的字串，例如「林子耕(0183253)」。未登入為「(未登入)」。</summary>
    string Name { get; }

    /// <summary>員工編號。未登入為空字串。</summary>
    string EmpNo { get; }

    /// <summary>姓名，供畫面問候語使用。</summary>
    string DisplayName { get; }

    /// <summary>角色。未登入或 Cookie 內容異常時為 null。</summary>
    UserRole? Role { get; }

    bool IsAuthenticated { get; }

    /// <summary>管理者：所有畫面與功能。</summary>
    bool IsAdmin { get; }

    /// <summary>主管或管理者：六張主要清單可以無條件新增／修改／刪除。</summary>
    bool IsManagerOrAbove { get; }
}

/// <summary>Claim 型別名稱。字串只在這裡出現一次，簽發與讀取共用。</summary>
public static class PdClaims
{
    public const string EmpNo = "pd:empno";
}

/// <inheritdoc />
public sealed class HttpContextCurrentUser : ICurrentUser
{
    /// <summary>尚未登入時寫入軌跡的值。</summary>
    public const string Anonymous = "(未登入)";

    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string EmpNo => Principal?.FindFirst(PdClaims.EmpNo)?.Value ?? "";

    public string DisplayName
    {
        get
        {
            var name = Principal?.Identity?.Name;
            return string.IsNullOrWhiteSpace(name) ? EmpNo : name;
        }
    }

    public UserRole? Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirst(ClaimTypes.Role)?.Value, out var role)
            ? role
            : null;

    public bool IsAdmin => Role == UserRole.Admin;

    public bool IsManagerOrAbove => Role is UserRole.Admin or UserRole.Manager;

    public string Name
    {
        get
        {
            if (!IsAuthenticated) return Anonymous;
            var empNo = EmpNo;
            var name = Principal?.Identity?.Name ?? "";
            if (string.IsNullOrWhiteSpace(empNo)) return string.IsNullOrWhiteSpace(name) ? Anonymous : name;
            return string.IsNullOrWhiteSpace(name) ? empNo : $"{name}({empNo})";
        }
    }
}

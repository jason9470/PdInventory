using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Helpers;
using PdInventory.Models;

namespace PdInventory.Controllers;

/// <summary>
/// 登入與登出。系統不保管帳號密碼，一律交給公司 AD 驗證（見 Helpers/IEmployeeDirectory.cs），
/// 本系統只負責記下「這個人是誰、是什麼角色、負責哪些資產」。
/// </summary>
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly IEmployeeDirectory _directory;

    public AccountController(AppDbContext db, IEmployeeDirectory directory)
    {
        _db = db;
        _directory = directory;
    }

    public async Task<IActionResult> Login(string? returnUrl)
    {
        await PrepareLoginViewAsync(returnUrl);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string account, string? password, string? returnUrl)
    {
        var info = await _directory.AuthenticateAsync(account, password);
        if (info is null)
        {
            // 帳號不存在與密碼錯誤給同一個訊息，避免旁人藉此確認哪些帳號存在
            TempData["Error"] = "帳號、密碼或存取權限不正確。";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        await SignInAsync(info);
        return SafeRedirect(returnUrl);
    }

    private async Task PrepareLoginViewAsync(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.RequiresPassword = _directory.RequiresPassword;
        // 模擬模式列出已建檔的帳號，測試時直接點選即可，不必記員工編號
        ViewBag.KnownUsers = _directory.RequiresPassword
            ? new List<AppUser>()
            : await _db.AppUsers.OrderBy(u => u.EmpNo).ToListAsync();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    /// <summary>已登入但角色不足時的畫面。</summary>
    public IActionResult Denied() => View();

    /// <summary>
    /// 建立（或更新）使用者資料並簽發驗證 Cookie。
    /// 第一次登入的人自動建檔，角色給最低的資產負責人且名下沒有資產，
    /// 也就是只能看不能改；要能改什麼由管理者在權限設定畫面指定。
    /// </summary>
    private async Task SignInAsync(EmployeeInfo info)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.EmpNo == info.EmpNo);
        var isNew = user is null;
        user ??= new AppUser { EmpNo = info.EmpNo, Role = UserRole.AssetOwner };

        // 員工目錄沒回姓名時（模擬模式）沿用既有值，不要把已知的姓名洗成空白
        if (!string.IsNullOrWhiteSpace(info.EmpName)) user.EmpName = info.EmpName;
        user.LastLoginAt = DateTime.Now;
        if (isNew) _db.AppUsers.Add(user);

        var principal = BuildPrincipal(user);
        // 先掛上身分再存檔，這樣自動建檔的軌跡會記成本人，而不是「(未登入)」
        HttpContext.User = principal;
        await _db.SaveChangesAsync();

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false });
    }

    private static ClaimsPrincipal BuildPrincipal(AppUser user)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, user.EmpName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(PdClaims.EmpNo, user.EmpNo),
        ], CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);

        return new ClaimsPrincipal(identity);
    }

    /// <summary>只接受站內網址，避免被構造成導向外部網站的跳板。</summary>
    private IActionResult SafeRedirect(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home");
}

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
    private readonly UserProvisioning _provisioning;

    public AccountController(AppDbContext db, IEmployeeDirectory directory,
                             UserProvisioning provisioning)
    {
        _db = db;
        _directory = directory;
        _provisioning = provisioning;
    }

    public async Task<IActionResult> Login(string? returnUrl)
    {
        await PrepareLoginViewAsync(returnUrl);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    /// <param name="displayName">
    /// 只有模擬模式看得到的姓名欄位。模擬的員工目錄回不出姓名，而姓名是人員表建檔的必要資訊，
    /// 沒有它就無法在沒有 AD 的環境下驗證「第一次登入自動建檔」這條流程。正式模式忽略這個值。
    /// </param>
    public async Task<IActionResult> Login(string account, string? password,
                                           string? displayName, string? returnUrl)
    {
        var info = await _directory.AuthenticateAsync(account, password);
        if (info is null)
        {
            // 帳號不存在與密碼錯誤給同一個訊息，避免旁人藉此確認哪些帳號存在
            TempData["Error"] = "帳號、密碼或存取權限不正確。";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        // 被管理者停用的人：密碼是對的，所以不必再遮掩原因，直接說明並請他找管理者，
        // 否則他只會看到「帳號密碼不正確」而反覆重試。
        if (await _provisioning.IsDeactivatedAsync(info.EmpNo))
        {
            TempData["Error"] = UserProvisioning.DeactivatedMessage;
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        if (!_directory.RequiresPassword && !string.IsNullOrWhiteSpace(displayName))
            info = info with { EmpName = displayName.Trim() };

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
        ForgetSessionState();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// 清掉 Session。目前裡面只有清單頁的搜尋記憶（見 <see cref="SearchMemory"/>），
    /// 記的是「這個人剛剛看到哪一筆資產」。
    ///
    /// 登出與登入都要清：SignOutAsync 只處理驗證 Cookie，不會動到 Session，
    /// 因此不清的話，下一個在同一台機器登入的人一進清單頁，搜尋欄就已經填著
    /// 前一個人看過的資產編號。
    ///
    /// 整個清掉而不是只移除那一個鍵：Session 放的都是「這次操作到哪裡」這類東西，
    /// 換身分時一律作廢，日後多放別的也不必回來補這一行。
    /// </summary>
    private void ForgetSessionState() => HttpContext.Session.Clear();

    /// <summary>已登入但角色不足時的畫面。</summary>
    public IActionResult Denied() => View();

    /// <summary>
    /// 建立（或更新）使用者資料並簽發驗證 Cookie。
    ///
    /// 第一次登入的人在**人員表與使用者帳號**都自動建檔（見 UserProvisioning）：
    /// 人員表帶入員編、姓名與固定的部門與備註（沒有科別），帳號給一般使用者，
    /// 也就是只能看不能改；要能改什麼由管理者在權限設定 → 人員指定科別與角色。
    ///
    /// 若管理者已經先在人員表建過檔，這裡就直接對應到那筆已設好的權限。
    /// </summary>
    private async Task SignInAsync(EmployeeInfo info)
    {
        // 上一個人沒按登出就直接換人登入時，這裡把殘留的記憶清掉（理由見 ForgetSessionState）
        ForgetSessionState();

        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.EmpNo == info.EmpNo);
        var isNew = user is null;
        user ??= new AppUser { EmpNo = info.EmpNo, Role = UserRole.AssetOwner };

        // 員工目錄沒回姓名時（模擬模式）沿用既有值，不要把已知的姓名洗成空白
        if (!string.IsNullOrWhiteSpace(info.EmpName)) user.EmpName = info.EmpName;
        user.LastLoginAt = DateTime.Now;
        if (isNew) _db.AppUsers.Add(user);

        // 人員表那一邊。姓名只在建檔時寫入，之後不隨員工目錄更新——
        // 人員表的姓名就是各表單欄位裡存的值，自動改名會讓那些資料變成孤兒。
        await _provisioning.EnsureEmployeeForLoginAsync(
            info with { EmpName = string.IsNullOrWhiteSpace(info.EmpName) ? user.EmpName : info.EmpName });

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

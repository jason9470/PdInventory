using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Helpers;
using PdInventory.Models;
using PdInventory.Models.ViewModels;

namespace PdInventory.Controllers;

/// <summary>
/// 權限設定：調整使用者的角色。
///
/// 0917 起能修改哪些系統由人員表的科別決定（見 <see cref="SectionScope"/>），
/// 這裡只唯讀呈現結果。同一件事只有一個地方能改：角色在這裡、人屬於哪個科在人員表、
/// 科負責哪些系統在科別表。
///
/// 使用者不在這裡新增也不在這裡刪除，兩者都由人員表那一邊帶動
/// （見 Helpers/UserProvisioning.cs）：管理者在人員表建檔時一併建立帳號，
/// 或是那個人第一次登入時自動建檔；人員表刪除時帳號一起停用。
/// </summary>
[Authorize(Policy = Policies.Admin)]
public class PermissionsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public PermissionsController(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int? userId)
    {
        // 科別在人員表那一邊，畫面要顯示就得補查（以員工編號相認）
        var employees = await _db.Employees
            .Include(e => e.Section)
            .Where(e => e.EmpNo != "")
            .ToDictionaryAsync(e => e.EmpNo);

        var users = await _db.AppUsers.ToListAsync();

        var model = new PermissionsViewModel
        {
            // 依科別排，同一個科的人排在一起；科別內管理者在前
            Users = users
                .OrderBy(u => employees.GetValueOrDefault(u.EmpNo)?.Section?.SortOrder ?? int.MaxValue)
                .ThenByDescending(u => u.Role)
                .ThenBy(u => u.EmpNo)
                .ToList(),
            Employees = employees,
            Scope = await SectionScope.LoadAsync(_db),
            TotalSystems = await _db.InfoSystems.CountAsync(),
        };

        if (userId is not null)
        {
            model.Selected = model.Users.FirstOrDefault(u => u.Id == userId);
            if (model.Selected is null) return NotFound();
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int id, UserRole role, Guid rowVersion)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var blockReason = await RoleChangeBlockReason(user, role);
        if (blockReason is not null)
        {
            TempData["Error"] = blockReason;
            return RedirectToAction(nameof(Index), new { userId = id });
        }

        user.Role = role;

        _db.Entry(user).Property(e => e.RowVersion).OriginalValue = rowVersion;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "這位使用者的權限在你編輯期間已被其他管理者修改，畫面已重新載入最新內容，請確認後再存一次。";
            return RedirectToAction(nameof(Index), new { userId = id });
        }

        TempData["Message"] = $"已更新「{user.Label}」的角色：{RoleDisplay.Name(role)}";
        return RedirectToAction(nameof(Index), new { userId = id });
    }

    /// <summary>
    /// 擋掉會讓系統再也沒有人能管理的兩種操作：把自己降級，或降掉最後一個管理者。
    /// 回傳 null 代表允許，否則回傳要顯示給使用者的原因。
    /// </summary>
    private async Task<string?> RoleChangeBlockReason(AppUser user, UserRole role)
    {
        if (user.Role == UserRole.Admin && role != UserRole.Admin)
        {
            if (user.EmpNo == _currentUser.EmpNo)
                return "不能調降自己的角色，請由另一位管理者操作。";

            var adminCount = await _db.AppUsers.CountAsync(u => u.Role == UserRole.Admin);
            if (adminCount <= 1)
                return "系統至少要保留一位管理者，無法調降這個帳號。";
        }

        return null;
    }
}

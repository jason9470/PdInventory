using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Helpers;
using PdInventory.Models;
using PdInventory.Models.ViewModels;

namespace PdInventory.Controllers;

/// <summary>
/// 權限設定：調整使用者的角色，以及資產負責人名下的資產。
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
        var model = new PermissionsViewModel
        {
            Users = await _db.AppUsers
                .OrderByDescending(u => u.Role)
                .ThenBy(u => u.EmpNo)
                .ToListAsync(),
            // 組別／科別在人員表那一邊，畫面要顯示就得補查（以員工編號相認）
            Employees = await _db.Employees
                .Where(e => e.EmpNo != "")
                .ToDictionaryAsync(e => e.EmpNo),
            Assets = await _db.InfoSystems
                .OrderBy(s => s.SystemCode)
                .ToListAsync(),
        };

        if (userId is not null)
        {
            model.Selected = model.Users.FirstOrDefault(u => u.Id == userId);
            if (model.Selected is null) return NotFound();

            model.OwnedAssetIds = await _db.AssetOwners
                .Where(a => a.AppUserId == userId)
                .Select(a => a.InfoSystemId)
                .ToHashSetAsync();
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int id, UserRole role, int[] assetIds, Guid rowVersion)
    {
        var user = await _db.AppUsers
            .Include(u => u.OwnedAssets)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var blockReason = await RoleChangeBlockReason(user, role);
        if (blockReason is not null)
        {
            TempData["Error"] = blockReason;
            return RedirectToAction(nameof(Index), new { userId = id });
        }

        user.Role = role;
        ApplyAssetOwnership(user, assetIds);

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

        TempData["Message"] = $"已更新「{user.Label}」的權限：{RoleDisplay.Name(role)}，負責 {assetIds.Length} 項資產";
        return RedirectToAction(nameof(Index), new { userId = id });
    }

    /// <summary>
    /// 把勾選結果套用到授權明細：只加新勾的、只刪取消勾的，
    /// 不整批刪除重建，這樣未變動的列不會產生無謂的異動。
    /// </summary>
    private void ApplyAssetOwnership(AppUser user, int[] assetIds)
    {
        var wanted = assetIds.ToHashSet();

        foreach (var existing in user.OwnedAssets.Where(a => !wanted.Contains(a.InfoSystemId)).ToList())
        {
            user.OwnedAssets.Remove(existing);
            _db.AssetOwners.Remove(existing);
        }

        var current = user.OwnedAssets.Select(a => a.InfoSystemId).ToHashSet();
        foreach (var assetId in wanted.Where(assetId => !current.Contains(assetId)))
            user.OwnedAssets.Add(new AssetOwner { AppUserId = user.Id, InfoSystemId = assetId });
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

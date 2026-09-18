using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>
/// 舊的權限設定網址。0918 起角色併進人員表單（權限設定 → 人員，見 <see cref="EmployeesController"/>），
/// 這裡只負責把舊書籤與舊連結轉過去：帶了 userId 就直接開那個人的編輯畫面。
/// </summary>
// 舊網址只負責轉到人員畫面
[Authorize(Policy = Policies.ViewMaintenance)]
public class PermissionsController : Controller
{
    private readonly AppDbContext _db;

    public PermissionsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? userId)
    {
        if (userId is not null)
        {
            var empNo = await _db.AppUsers.Where(u => u.Id == userId).Select(u => u.EmpNo).FirstOrDefaultAsync();
            var employeeId = empNo is null ? null
                : await _db.Employees.Where(e => e.EmpNo == empNo).Select(e => (int?)e.Id).FirstOrDefaultAsync();
            if (employeeId is not null)
                return RedirectToAction("Edit", "Employees", new { id = employeeId });
        }

        return RedirectToAction("Index", "Employees");
    }
}

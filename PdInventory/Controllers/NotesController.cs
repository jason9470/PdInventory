using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;
using PdInventory.Helpers;

namespace PdInventory.Controllers;

/// <summary>
/// 盤點重點。全站導覽列的「?」按鈕點開一個彈跳視窗，視窗裡的三個畫面
/// （清單／內容／編輯）都由這裡以部分檢視回傳，由 site.js 換進去。
///
/// 之所以走部分檢視而不是各自一個完整頁面：這個功能要能從**任何一頁**打開，
/// 而且看完就關掉回到原本的工作，導頁會把使用者手上的搜尋條件與捲動位置弄丟。
///
/// 讀取所有登入者都可以；新增與修改限管理者——按鈕會依角色隱藏，但那只是操作
/// 防呆，真正的把關是這裡的 [Authorize]。
/// </summary>
[Authorize(Policy = Policies.ViewAssets)]
public class NotesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public NotesController(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>清單：標題與更新時間，新的在前面。</summary>
    public async Task<IActionResult> List()
    {
        var notes = await _db.InventoryNotes
            .OrderByDescending(n => n.UpdatedAt ?? n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .ToListAsync();

        return PartialView("_List", notes);
    }

    /// <summary>單筆內容。</summary>
    public async Task<IActionResult> Detail(int id)
    {
        var note = await _db.InventoryNotes.FindAsync(id);
        return note is null ? NotFound() : PartialView("_Detail", note);
    }

    /// <param name="id">留空代表新增。</param>
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Form(int? id)
    {
        if (id is null) return PartialView("_Form", new InventoryNote());

        var note = await _db.InventoryNotes.FindAsync(id);
        return note is null ? NotFound() : PartialView("_Form", note);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Save(InventoryNote model)
    {
        if (!ModelState.IsValid) return PartialView("_Form", model);

        if (model.Id == 0)
        {
            _db.InventoryNotes.Add(model);
            await _db.SaveChangesAsync();
            return await Detail(model.Id);
        }

        var existing = await _db.InventoryNotes.FindAsync(model.Id);
        if (existing is null) return NotFound();

        existing.Title = model.Title;
        existing.Content = model.Content;
        // 以畫面載入當下的權杖比對：期間內被別人存過就擋下，不做靜默覆蓋
        _db.Entry(existing).Property(e => e.RowVersion).OriginalValue = model.RowVersion;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "這筆重點在你編輯期間已被其他管理者修改，"
                                       + "請關掉視窗重新打開，確認最新內容後再存一次。");
            return PartialView("_Form", model);
        }

        return await Detail(existing.Id);
    }
}

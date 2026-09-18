using Microsoft.AspNetCore.Mvc;

namespace PdInventory.Helpers;

/// <summary>
/// 清單頁搜尋條件的 Session 記憶。六張清單頁共用同一格，
/// 因為 SW-XXX 是各表都有的欄位，可用它把不同表單串在一起：
/// 在 SW 清單點開某筆後切到 DA 清單，搜尋欄會自動帶入同一個 SW-XXX。
///
/// 儲存：按下[搜尋]時存搜尋欄文字；點[檢視]/[編輯]時存該筆的資產編號。
/// 清除：按下[清除]、[取消]、[回列表] 時（這些連結都帶 clear）。
/// 使用：直接進入任一清單頁（網址沒有 q）時帶出記憶值並套用篩選。
///
/// 0918 起組別（<c>SW-權責單位</c>）的篩選也一起記，規則相同——同一顆[搜尋]送出、
/// 同一顆[清除]清掉。兩者分開存，但六張清單共用，切換清單時篩選跟著走。
/// </summary>
public static class SearchMemory
{
    private const string Key = "search";
    private const string TeamKey = "searchTeam";

    /// <summary>決定本次清單頁要用的搜尋字串，並同步更新記憶。</summary>
    /// <remarks>
    /// clear 與 q 都以「網址有沒有這個參數」判斷，不用 action 參數：
    /// bool 參數只接受 true/false，clear=1 會綁不進來而靜默失效。
    /// </remarks>
    public static string? ResolveSearch(this Controller controller, string? q)
    {
        var session = controller.HttpContext.Session;

        // [清除]/[取消]/[回列表] 的連結都帶 clear
        if (controller.Request.Query.ContainsKey("clear"))
        {
            session.Remove(Key);
            return null;
        }

        // 網址帶 q 代表使用者按了[搜尋]（空字串也算，等同清掉條件）
        if (controller.Request.Query.ContainsKey("q"))
        {
            if (string.IsNullOrWhiteSpace(q)) session.Remove(Key);
            else session.SetString(Key, q);
            return q;
        }

        return session.GetString(Key);
    }

    /// <summary>決定本次清單頁要用的組別，並同步更新記憶。規則同 <see cref="ResolveSearch"/>。</summary>
    public static string? ResolveTeam(this Controller controller, string? team)
    {
        var session = controller.HttpContext.Session;

        if (controller.Request.Query.ContainsKey("clear"))
        {
            session.Remove(TeamKey);
            return null;
        }

        // 下拉與搜尋欄在同一個表單裡，按[搜尋]時一定會一起送出（選「全部組別」是空字串）
        if (controller.Request.Query.ContainsKey("team"))
        {
            if (string.IsNullOrWhiteSpace(team)) session.Remove(TeamKey);
            else session.SetString(TeamKey, team);
            return team;
        }

        return session.GetString(TeamKey);
    }

    /// <summary>點[檢視]/[編輯]時記住該筆的資產編號，切到其他清單頁即可沿用。</summary>
    public static void RememberSearch(this Controller controller, string? value)
    {
        var session = controller.HttpContext.Session;

        if (string.IsNullOrWhiteSpace(value)) session.Remove(Key);
        else session.SetString(Key, value);
    }
}

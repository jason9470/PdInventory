namespace PdInventory.Helpers;

/// <summary>
/// 目前操作者。軌跡與軟刪除都需要「是誰做的」，但系統尚未導入驗證機制，
/// 因此先以這個介面把接縫留好：日後接上登入後，只需改 HttpContextCurrentUser
/// 的實作回傳真實帳號，其餘程式碼不必更動。
/// </summary>
public interface ICurrentUser
{
    string Name { get; }
}

/// <inheritdoc />
public sealed class HttpContextCurrentUser : ICurrentUser
{
    /// <summary>尚未導入驗證前的預留值。</summary>
    public const string Anonymous = "(未登入)";

    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public string Name
    {
        get
        {
            // 導入驗證後這裡會拿到真實帳號；在那之前一律記為未登入。
            var name = _accessor.HttpContext?.User?.Identity?.Name;
            return string.IsNullOrWhiteSpace(name) ? Anonymous : name;
        }
    }
}

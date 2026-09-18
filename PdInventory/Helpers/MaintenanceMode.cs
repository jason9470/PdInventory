namespace PdInventory.Helpers;

/// <summary>
/// 網站維護中模式。開啟時所有請求一律導到「網站維護中」畫面（<c>/Maintenance</c>），
/// 登入與否、角色為何都一樣。
///
/// 開關是設定值 <c>Maintenance:Enabled</c>。正式環境在 IIS 的 <c>web.config</c> 以環境變數切換：
/// <code>&lt;environmentVariable name="Maintenance__Enabled" value="true" /&gt;</code>
/// （雙底線就是設定階層的冒號）。IIS 偵測到 web.config 變更會自動重啟網站，改完存檔即生效，
/// 不必重新部署。開發環境改 appsettings.json 的同名設定即可。
///
/// 為什麼不用 IIS 內建的 <c>app_offline.htm</c>：那個做法會把網站整個卸載，
/// 連啟動時的 migration 也不會跑；而且要放檔案、移檔案，不如在 web.config 改一個值直覺。
/// </summary>
public sealed class MaintenanceOptions
{
    public const string SectionName = "Maintenance";

    /// <summary>true 時整個網站導到維護中畫面。</summary>
    public bool Enabled { get; set; }

    /// <summary>維護畫面上額外顯示的說明（選填），例如預計恢復時間。</summary>
    public string Message { get; set; } = "";
}

public static class MaintenanceModeExtensions
{
    /// <summary>維護中畫面的網址。</summary>
    public const string Path = "/Maintenance";

    /// <summary>
    /// 放在管線最前面（路由、驗證之前）：維護期間不該再碰資料庫，也不必知道是誰。
    /// 維護畫面本身不引用任何靜態檔（樣式內嵌），因此不需要替 css/js 開例外。
    /// </summary>
    public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder app, MaintenanceOptions options)
    {
        if (!options.Enabled) return app;

        return app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(Path, StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            context.Response.Redirect(Path);
        });
    }
}

using System.Net;
using System.Text.RegularExpressions;

namespace PdInventory.Helpers;

/// <summary>員工目錄回傳的身分。本系統只需要這兩個欄位。</summary>
public sealed record EmployeeInfo(string EmpNo, string EmpName);

/// <summary>
/// 員工目錄。帳號密碼不由本系統保管，登入時向公司的身分端點問「現在這個人是誰」。
///
/// 之所以抽成介面：那個端點是靠 IIS 整合驗證認出「發出 request 的人」，
/// 因此由伺服器端代打、由瀏覽器端直接打、或改走本站的 Windows 驗證，
/// 認到的會是不同的人。實際哪一種可行必須在公司網內實測，
/// 換實作時只要改 appsettings 的 Employee:Mode，其餘程式不必更動。
/// </summary>
public interface IEmployeeDirectory
{
    /// <summary>是否要使用者自己在登入頁指定身分（模擬模式才會是 true）。</summary>
    bool IsSimulated { get; }

    /// <summary>問出目前操作者是誰。取不到（未在公司網內、端點不通）回傳 null。</summary>
    Task<EmployeeInfo?> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>以員工編號取得身分，供模擬登入使用。</summary>
    Task<EmployeeInfo?> FindAsync(string empNo, CancellationToken cancellationToken = default);
}

/// <summary>appsettings 的 Employee 區段。</summary>
public sealed class EmployeeDirectoryOptions
{
    public const string SectionName = "Employee";

    /// <summary>Remote＝呼叫公司端點；Simulated＝在登入頁自行挑選身分（開發與測試用）。</summary>
    public string Mode { get; set; } = "Simulated";

    public string PersonInfoUrl { get; set; } = "";

    public bool IsSimulated =>
        !string.Equals(Mode, "Remote", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// 從身分端點的回應中取出 EmpNo 與 EmpName。
///
/// 回應長這樣（外面包著 html，中間是一段 JavaScript 變數宣告）：
///   var userInfo = { EmpNo:'0183253', EmpName:'林子耕', DeptName:'...', ... };
/// 不當成 JSON 解析，因為它不是合法 JSON（沒有雙引號、鍵沒有引號）。
/// </summary>
public static class PersonInfoParser
{
    public static EmployeeInfo? Parse(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        var empNo = ReadField(body, "EmpNo");
        var empName = ReadField(body, "EmpName");

        // 沒有員工編號就無法識別身分，寧可當作沒登入
        return string.IsNullOrWhiteSpace(empNo) ? null : new EmployeeInfo(empNo, empName);
    }

    private static string ReadField(string body, string key)
    {
        // 單引號與雙引號都接受，欄位間的空白數量不拘
        var match = Regex.Match(body, key + @"\s*:\s*['""]([^'""]*)['""]",
                                RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        return match.Success ? match.Groups[1].Value.Trim() : "";
    }
}

/// <summary>呼叫公司身分端點取得目前操作者。</summary>
public sealed class RemoteEmployeeDirectory : IEmployeeDirectory
{
    private readonly HttpClient _http;
    private readonly EmployeeDirectoryOptions _options;
    private readonly ILogger<RemoteEmployeeDirectory> _logger;

    public RemoteEmployeeDirectory(HttpClient http, EmployeeDirectoryOptions options,
                                   ILogger<RemoteEmployeeDirectory> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public bool IsSimulated => false;

    public async Task<EmployeeInfo?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.PersonInfoUrl))
        {
            _logger.LogError("未設定 Employee:PersonInfoUrl，無法取得身分");
            return null;
        }

        try
        {
            var body = await _http.GetStringAsync(_options.PersonInfoUrl, cancellationToken);
            var info = PersonInfoParser.Parse(body);
            if (info is null)
                _logger.LogWarning("身分端點回應中找不到 EmpNo：{Body}", Truncate(body));
            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "呼叫身分端點失敗：{Url}", _options.PersonInfoUrl);
            return null;
        }
    }

    /// <summary>端點只回「目前是誰」，無法指定他人，因此不支援。</summary>
    public Task<EmployeeInfo?> FindAsync(string empNo, CancellationToken cancellationToken = default) =>
        Task.FromResult<EmployeeInfo?>(null);

    private static string Truncate(string text) =>
        text.Length <= 500 ? text : text[..500] + "…";
}

/// <summary>
/// 模擬員工目錄：由登入頁指定身分。
/// 測試帳號（1111111 等）在公司目錄裡並不存在，只能靠這個模式登入；
/// 正式環境把 Employee:Mode 設為 Remote 即可停用。
/// </summary>
public sealed class SimulatedEmployeeDirectory : IEmployeeDirectory
{
    public bool IsSimulated => true;

    /// <summary>模擬模式沒有「自動認人」這回事，一律要在登入頁挑。</summary>
    public Task<EmployeeInfo?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<EmployeeInfo?>(null);

    /// <summary>姓名交由呼叫端從 AppUsers 補齊，這裡只確認編號格式可用。</summary>
    public Task<EmployeeInfo?> FindAsync(string empNo, CancellationToken cancellationToken = default)
    {
        empNo = (empNo ?? "").Trim();
        return Task.FromResult(string.IsNullOrEmpty(empNo) ? null : new EmployeeInfo(empNo, ""));
    }
}

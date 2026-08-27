using System.DirectoryServices.Protocols;
using System.Net;
using System.Text.RegularExpressions;

namespace PdInventory.Helpers;

/// <summary>員工目錄回傳的身分。本系統只需要這兩個欄位。</summary>
public sealed record EmployeeInfo(string EmpNo, string EmpName);

/// <summary>
/// 員工目錄。帳號密碼不由本系統保管，一律交給公司的 AD 驗證。
///
/// 抽成介面是為了讓測試環境能在沒有 AD 的情況下運作：把 Employee:Mode 設為
/// Simulated 就改用登入頁挑選身分的假實作，其餘程式碼完全不必更動。
/// </summary>
public interface IEmployeeDirectory
{
    /// <summary>登入頁要不要顯示密碼欄位。模擬模式不需要密碼。</summary>
    bool RequiresPassword { get; }

    /// <summary>
    /// 驗證帳號密碼並取得身分。失敗一律回傳 null，不區分「帳號不存在」與「密碼錯誤」——
    /// 兩者若給出不同訊息，等於幫忙確認哪些帳號存在。
    /// </summary>
    Task<EmployeeInfo?> AuthenticateAsync(string account, string? password,
                                          CancellationToken cancellationToken = default);
}

/// <summary>appsettings 的 Employee 區段。</summary>
public sealed class EmployeeDirectoryOptions
{
    public const string SectionName = "Employee";

    /// <summary>Ldap＝向公司 AD 驗證；Simulated＝在登入頁自行挑選身分（開發與測試用）。</summary>
    public string Mode { get; set; } = "Simulated";

    public LdapOptions Ldap { get; set; } = new();

    public bool IsSimulated =>
        !string.Equals(Mode, "Ldap", StringComparison.OrdinalIgnoreCase);
}

/// <summary>AD 連線與查詢設定。這些值請洽網域管理者取得。</summary>
public sealed class LdapOptions
{
    /// <summary>網域控制站或網域的 DNS 名稱。用 FQDN，不要用 IP，否則 LDAPS 憑證驗證會失敗。</summary>
    public string Host { get; set; } = "";

    /// <summary>LDAP 389、LDAPS 636。</summary>
    public int Port { get; set; } = 636;

    /// <summary>是否走 LDAPS。正式環境建議開啟，且 Web Server 必須信任簽發憑證的 CA。</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>bind 時附在帳號上的網域，例如 brk.corp.yuanta.com 或短名 BRK。</summary>
    public string Domain { get; set; } = "";

    /// <summary>搜尋起點，例如 DC=brk,DC=corp,DC=yuanta,DC=com。</summary>
    public string BaseDn { get; set; } = "";

    /// <summary>以哪個屬性比對登入帳號。內部帳號通常是 sAMAccountName。</summary>
    public string AccountAttribute { get; set; } = "sAMAccountName";

    /// <summary>只允許特定群組登入時填入群組 DN；留空表示不做群組檢查。</summary>
    public string RequiredGroupDn { get; set; } = "";

    /// <summary>
    /// 員工編號的位數。AD 的帳號是 183253，但本系統既有資料是 0183253（七碼、前面補零），
    /// 兩者不一致會讓同一個人變成兩筆使用者，因此登入時統一補到這個長度。
    /// </summary>
    public int EmpNoLength { get; set; } = 7;

    /// <summary>Negotiate（預設，等同教學文件的 AuthenticationTypes.Secure）或 Basic（僅限 LDAPS）。</summary>
    public string AuthType { get; set; } = "Negotiate";

    public int TimeoutSeconds { get; set; } = 10;
}

/// <summary>
/// 向 AD 驗證。流程與公司提供的教學文件一致：先用使用者本人的帳密 bind
/// 以確認密碼正確，再用同一條已驗證的連線搜尋該帳號的屬性。
///
/// 與文件的差異：文件是寫給 .NET Framework 的 MVC 5，用 System.DirectoryServices
/// 的 DirectoryEntry；本專案是 ASP.NET Core (.NET 10)，改用 .NET 支援的
/// System.DirectoryServices.Protocols。連線流程與安全考量相同。
/// </summary>
public sealed class LdapEmployeeDirectory : IEmployeeDirectory
{
    private readonly LdapOptions _options;
    private readonly ILogger<LdapEmployeeDirectory> _logger;

    public LdapEmployeeDirectory(EmployeeDirectoryOptions options,
                                 ILogger<LdapEmployeeDirectory> logger)
    {
        _options = options.Ldap;
        _logger = logger;
    }

    public bool RequiresPassword => true;

    public Task<EmployeeInfo?> AuthenticateAsync(string account, string? password,
                                                 CancellationToken cancellationToken = default)
    {
        account = (account ?? "").Trim();
        if (account.Length == 0 || string.IsNullOrEmpty(password))
            return Task.FromResult<EmployeeInfo?>(null);

        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.BaseDn))
        {
            _logger.LogError("未設定 Employee:Ldap:Host 或 BaseDn，無法向 AD 驗證");
            return Task.FromResult<EmployeeInfo?>(null);
        }

        // LdapConnection 沒有真正的非同步版本，包成 Task 只是為了讓呼叫端的寫法一致
        return Task.FromResult(Authenticate(account, password));
    }

    private EmployeeInfo? Authenticate(string account, string password)
    {
        try
        {
            using var connection = new LdapConnection(
                new LdapDirectoryIdentifier(_options.Host, _options.Port));

            connection.SessionOptions.ProtocolVersion = 3;
            connection.SessionOptions.SecureSocketLayer = _options.UseSsl;
            connection.AuthType = string.Equals(_options.AuthType, "Basic", StringComparison.OrdinalIgnoreCase)
                ? System.DirectoryServices.Protocols.AuthType.Basic
                : System.DirectoryServices.Protocols.AuthType.Negotiate;
            connection.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            // 這一步就是密碼驗證：bind 不過就是帳號或密碼不對
            connection.Bind(new NetworkCredential(account, password, _options.Domain));

            var request = new SearchRequest(
                _options.BaseDn,
                BuildFilter(account),
                SearchScope.Subtree,
                "sAMAccountName", "displayName", "cn", "memberOf");

            if (connection.SendRequest(request) is not SearchResponse response
                || response.Entries.Count == 0)
            {
                _logger.LogWarning("帳號 {Account} 通過 bind 但在 {BaseDn} 找不到對應項目，"
                                 + "請確認 BaseDn 與帳號屬性設定", account, _options.BaseDn);
                return null;
            }

            var entry = response.Entries[0];

            if (!string.IsNullOrWhiteSpace(_options.RequiredGroupDn)
                && !IsMemberOf(entry, _options.RequiredGroupDn))
            {
                _logger.LogInformation("帳號 {Account} 不屬於允許的群組，拒絕登入", account);
                return null;
            }

            var samAccount = FirstValue(entry, "sAMAccountName");
            return new EmployeeInfo(
                NormalizeEmpNo(samAccount.Length > 0 ? samAccount : account, _options.EmpNoLength),
                ExtractName(FirstValue(entry, "displayName"), FirstValue(entry, "cn"), account));
        }
        catch (LdapException ex)
        {
            // 帳密錯誤（ErrorCode 49）是最常見的情形，不算系統錯誤；連不到 AD 也會走到這裡，
            // 因此把錯誤碼記下來以便區分。絕對不記密碼。
            _logger.LogInformation("AD 驗證未通過（帳號 {Account}，LDAP ErrorCode {Code}）",
                                   account, ex.ErrorCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "向 AD 驗證時發生非預期錯誤（帳號 {Account}）", account);
            return null;
        }
    }

    private string BuildFilter(string account) =>
        $"(&(objectCategory=person)(objectClass=user)({_options.AccountAttribute}={Escape(account)}))";

    private static bool IsMemberOf(SearchResultEntry entry, string groupDn)
    {
        if (!entry.Attributes.Contains("memberOf")) return false;

        foreach (var value in entry.Attributes["memberOf"].GetValues(typeof(string)))
        {
            if (string.Equals(value as string, groupDn, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string FirstValue(SearchResultEntry entry, string attribute) =>
        entry.Attributes.Contains(attribute) && entry.Attributes[attribute].Count > 0
            ? entry.Attributes[attribute][0]?.ToString() ?? ""
            : "";

    /// <summary>
    /// AD 的 cn 是「183253 [林子耕]」這種格式，取中括號裡的姓名；
    /// displayName 若本身就是純姓名則直接使用。
    /// </summary>
    private static string ExtractName(string displayName, string cn, string fallback)
    {
        foreach (var candidate in new[] { displayName, cn })
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;

            var match = Regex.Match(candidate, @"\[([^\]]+)\]", RegexOptions.None, TimeSpan.FromSeconds(1));
            return match.Success ? match.Groups[1].Value.Trim() : candidate.Trim();
        }

        return fallback;
    }

    /// <summary>純數字帳號補足前導零，對齊本系統既有的員工編號格式。</summary>
    public static string NormalizeEmpNo(string account, int length)
    {
        account = (account ?? "").Trim();
        return account.Length > 0 && account.Length < length && account.All(char.IsDigit)
            ? account.PadLeft(length, '0')
            : account;
    }

    /// <summary>跳脫 LDAP 篩選條件的特殊字元，避免有人用帳號欄位注入查詢條件。</summary>
    private static string Escape(string value) => value
        .Replace("\\", "\\5c")
        .Replace("*", "\\2a")
        .Replace("(", "\\28")
        .Replace(")", "\\29")
        .Replace("\0", "\\00");
}

/// <summary>
/// 模擬員工目錄：由登入頁指定身分，不驗證密碼。
/// 測試帳號（1111111 等）在 AD 裡並不存在，只能靠這個模式登入；
/// 正式環境把 Employee:Mode 設為 Ldap 即可停用。
/// </summary>
public sealed class SimulatedEmployeeDirectory : IEmployeeDirectory
{
    public bool RequiresPassword => false;

    /// <summary>姓名交由呼叫端從 AppUsers 補齊，這裡只確認編號格式可用。</summary>
    public Task<EmployeeInfo?> AuthenticateAsync(string account, string? password,
                                                 CancellationToken cancellationToken = default)
    {
        account = (account ?? "").Trim();
        return Task.FromResult<EmployeeInfo?>(account.Length == 0 ? null : new EmployeeInfo(account, ""));
    }
}

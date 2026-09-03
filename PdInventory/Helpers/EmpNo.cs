namespace PdInventory.Helpers;

/// <summary>
/// 員工編號的格式。本系統一律用七碼、不足前面補零（0183253）。
///
/// 來源資料的位數並不一致：AD 的帳號是 183253，甲方名冊裡 7331、12033、0002876 都有。
/// 同一個人若在不同地方存成不同位數，就會變成兩筆對不起來的資料，因此進系統前一律過這裡。
/// </summary>
public static class EmpNo
{
    /// <summary>本系統的員工編號位數。</summary>
    public const int Length = 7;

    /// <summary>
    /// 純數字且不足指定位數就補零；其餘（空白、含英文字母、已達位數）原樣回傳——
    /// 看不懂的格式寧可保留原值，也不要自作聰明改掉。
    /// </summary>
    public static string Normalize(string? account, int length = Length)
    {
        account = (account ?? "").Trim();
        return account.Length > 0 && account.Length < length && account.All(char.IsDigit)
            ? account.PadLeft(length, '0')
            : account;
    }
}

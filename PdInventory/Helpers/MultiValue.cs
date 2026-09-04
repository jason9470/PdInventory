namespace PdInventory.Helpers;

/// <summary>
/// 多選欄位的存放格式：同一格用「/」分隔多個值（來源資料本來就這樣寫）。
///
/// 為什麼不另開關聯資料表：這些欄位來自甲方的 Excel，匯入與匯出都要維持原樣，
/// 拆成關聯表之後每次進出都得再拼回字串，對照關係反而更難核對。
///
/// 切開時有三個地雷，兩邊（畫面、後端統計）都必須用同一套規則，否則會對不上：
///   1. 括號裡的斜線不是分隔符——「總公司各單位(作業中心/國際法人部)」是一個值。
///   2. 「N/A」是一個完整的答案，不是 N 跟 A。
///   3. 來源試算表另外用過「;#」當分隔符（`自行開發;#委外開發`）。只切「;」的話
///      會在下一個值前面留一個「#」，變成選不到的髒值，因此要連井字號一起吃掉。
/// </summary>
public static class MultiValue
{
    public const string Separator = "/";

    /// <summary>N/A 在切開前先換成這個字元，切完再換回來。</summary>
    private const string NaPlaceholder = "";

    public static List<string> Split(string? value)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(value)) return result;

        var text = value.Replace("N/A", NaPlaceholder);
        var buffer = new System.Text.StringBuilder();
        var depth = 0;

        void Flush()
        {
            // TrimStart('#')：「;#」這種分隔符切完會在值的前面留一個井字號
            var token = buffer.ToString().Replace(NaPlaceholder, "N/A").Trim().TrimStart('#').Trim();
            if (token.Length > 0) result.Add(token);
            buffer.Clear();
        }

        foreach (var ch in text)
        {
            switch (ch)
            {
                case '(' or '（':
                    depth++;
                    buffer.Append(ch);
                    break;
                case ')' or '）':
                    depth = Math.Max(0, depth - 1);
                    buffer.Append(ch);
                    break;
                case '/' or ';' or '；' when depth == 0:
                    Flush();
                    break;
                default:
                    buffer.Append(ch);
                    break;
            }
        }

        Flush();
        return result;
    }

    public static string Join(IEnumerable<string> values) =>
        string.Join(Separator, values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()));

    /// <summary>
    /// 把一格的值整理成統一寫法：切開之後再以「/」接回去。
    /// 來源試算表混用了「/」「;」「;#」三種分隔符，存進來的值先過這裡才不會五花八門。
    /// </summary>
    public static string Normalize(string? value) => Join(Split(value));

    /// <summary>這一格裡有沒有這個值。單選欄位也適用（切完就是一個元素）。</summary>
    public static bool Contains(string? value, string item) =>
        Split(value).Any(v => string.Equals(v, item, StringComparison.Ordinal));
}

namespace PdInventory.Helpers;

/// <summary>
/// SW / DA / 系統盤點三張清單都會導向同一個[檢視]與[編輯]畫面，
/// 因此用網址上的 from 記住是從哪張清單進來的，[取消]/[回列表]才能回到原處。
/// </summary>
public static class ListSource
{
    /// <summary>沒有來源資訊時的預設清單。</summary>
    public const string Default = "Software";

    /// <summary>from 來自網址，限定在已知清單頁，避免產生無效連結。</summary>
    public static string Resolve(string? from) =>
        from is "Software" or "Data" or "Systems" ? from : Default;
}

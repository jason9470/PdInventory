using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>角色的中文名稱與說明。畫面上多處要顯示，集中一份避免各處寫法不一致。</summary>
public static class RoleDisplay
{
    /// <summary>
    /// 「資產負責人」0917 改稱「一般使用者」：原本的意思是「名下有資產的人」，
    /// 改成依科別授權之後已經沒有「名下」這回事。列舉值 AssetOwner 不動，既有資料不必轉換。
    /// </summary>
    public static string Name(UserRole role) => role switch
    {
        UserRole.Admin => "管理者",
        UserRole.Manager => "主管",
        _ => "一般使用者",
    };

    public static string Description(UserRole role) => role switch
    {
        UserRole.Admin => "所有畫面與功能：全部系統都能新增、修改、刪除，含維護資料、維護匯出與權限設定。",
        UserRole.Manager => "可檢視維護資料與權限設定（不能修改）；六張清單的權限與一般使用者相同。",
        _ => "六張清單都可檢視；只能修改自己科別負責的系統（組長為全組），不能新增或刪除。",
    };

    /// <summary>下拉選單用。順序由高到低，管理者排最前面。</summary>
    public static IEnumerable<UserRole> All =>
        [UserRole.Admin, UserRole.Manager, UserRole.AssetOwner];
}

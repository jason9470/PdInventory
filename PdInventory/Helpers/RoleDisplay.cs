using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>角色的中文名稱與說明。畫面上多處要顯示，集中一份避免各處寫法不一致。</summary>
public static class RoleDisplay
{
    public static string Name(UserRole role) => role switch
    {
        UserRole.Admin => "管理者",
        UserRole.Manager => "主管",
        _ => "資產負責人",
    };

    public static string Description(UserRole role) => role switch
    {
        UserRole.Admin => "所有畫面與功能，含維護資料、維護匯出與權限設定。",
        UserRole.Manager => "六張主要清單可新增／修改／刪除，不能使用維護資料、維護匯出與權限設定。",
        _ => "六張主要清單皆可檢視；只有名下資產可以修改／刪除，且不能新增。",
    };

    /// <summary>下拉選單用。順序由高到低，管理者排最前面。</summary>
    public static IEnumerable<UserRole> All =>
        [UserRole.Admin, UserRole.Manager, UserRole.AssetOwner];
}

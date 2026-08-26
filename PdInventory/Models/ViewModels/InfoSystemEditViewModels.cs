using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PdInventory.Models.ViewModels;

/// <summary>
/// SW／DA／系統盤點三個編輯區塊的 ViewModel。
///
/// 為什麼不直接綁 InfoSystem：那樣一次送出會綁定全部 98 個欄位，即使 Apply 方法
/// 只寫回其中一組，模型繫結階段仍然接受了不屬於這個畫面的輸入。改綁 ViewModel 之後，
/// 畫面能送什麼由型別本身決定，多送的欄位在繫結階段就被丟掉。
///
/// 欄位標題與驗證規則不在這裡重複宣告：[ModelMetadataType] 會讓 MVC 從 InfoSystem
/// 取同名屬性的中繼資料，[Display] 仍然只有實體那一份。
///
/// 這三個類別要與 Helpers/InfoSystemBlocks 的欄位分組保持一致；漏掉欄位會在啟動時
/// 由 InfoSystemBlocks.AssertViewModelsCoverAllFields 直接擲出例外，不會安靜地存不進去。
/// </summary>
public interface IInfoSystemBlockViewModel
{
    int Id { get; set; }
    Guid RowVersion { get; set; }
}

/// <summary>資訊資產清單－軟體(SW) 區塊。</summary>
/// <remarks>
/// 對應 InfoSystem 中以 Sw 開頭的欄位。
/// </remarks>
[ModelMetadataType(typeof(InfoSystem))]
public class SoftwareEditViewModel : IInfoSystemBlockViewModel
{
    public int Id { get; set; }

    /// <summary>並行權杖：畫面載入當下的值，存檔時比對是否已被他人改過。</summary>
    public Guid RowVersion { get; set; }

    public string SwStatus { get; set; } = "";
    public string SwAssetType { get; set; } = "";
    public string SwSystemCategory { get; set; } = "";
    public string SwAdIntegration { get; set; } = "";
    public string SwOsVersion { get; set; } = "";
    public string SwDbToolVersion { get; set; } = "";
    public string SwThirdPartyComponents { get; set; } = "";
    public string SwUserAccountGrant { get; set; } = "";
    public string SwProvidesAccountReport { get; set; } = "";
    public string SwRiskOwner { get; set; } = "";
    public string SwLocation { get; set; } = "";
    public string SwOwnerUnit { get; set; } = "";
    public string SwCustodianUnit { get; set; } = "";
    public string SwUserUnit { get; set; } = "";
    public string SwConfidentiality { get; set; } = "";
    public string SwIntegrity { get; set; } = "";
    public string SwAvailability { get; set; } = "";
    public string SwAssetValue { get; set; } = "";
    public string SwBusinessContact { get; set; } = "";
    public string SwAppManager { get; set; } = "";
    public string SwAppMaintainer { get; set; } = "";
    public string SwAppMaintainerDeputy { get; set; } = "";
    public string SwOperator { get; set; } = "";
    public string SwDevMode { get; set; } = "";
    public string SwMaintMode { get; set; } = "";
    public string SwVendor { get; set; } = "";
    public string SwLanguage { get; set; } = "";
    public string SwVersionControl { get; set; } = "";
    public string SwApRepoPath { get; set; } = "";
    public string SwDeployMethod { get; set; } = "";
    public string SwOpRepoPath { get; set; } = "";
    public string SwCodeAccess { get; set; } = "";
    public string SwDeveloper { get; set; } = "";
    public string SwDeployer { get; set; } = "";
    public string SwRpo { get; set; } = "";
    public string SwLocalBackup { get; set; } = "";
    public string SwLocalBackupType { get; set; } = "";
    public string SwLocalBackupFreq { get; set; } = "";
    public string SwRemoteBackup { get; set; } = "";
    public string SwRemoteBackupType { get; set; } = "";
    public string SwRemoteBackupFreq { get; set; } = "";
    public string SwLocalHa { get; set; } = "";
    public string SwLocalHaArch { get; set; } = "";
    public string SwRemoteHa { get; set; } = "";
    public string SwRemoteHaArch { get; set; } = "";
    public string SwRto { get; set; } = "";
    public string SwHasRecoveryPlan { get; set; } = "";
    public string SwHasDrDrill { get; set; } = "";
    public string SwRelatedSystems { get; set; } = "";
    public string SwRemark { get; set; } = "";
    public string SwHandlesPersonalData { get; set; } = "";
    public string SwHasUiAuth { get; set; } = "";
    public string SwKeepsPdTrail { get; set; } = "";
    public string SwProvidesApi { get; set; } = "";
    public string SwTrailLocation { get; set; } = "";
    public string SwTrailStorage { get; set; } = "";
    public string SwBusinessOwnerUnit { get; set; } = "";
    public string SwIsCoreSystem { get; set; } = "";
    public string SwReviewer { get; set; } = "";
    public string SwModifiedBy { get; set; } = "";
    public string SwModifiedTime { get; set; } = "";
}

/// <summary>資訊資產清單－資料(DA) 區塊。</summary>
/// <remarks>
/// 對應 InfoSystem 中以 Da 開頭的欄位。
/// </remarks>
[ModelMetadataType(typeof(InfoSystem))]
public class DataEditViewModel : IInfoSystemBlockViewModel
{
    public int Id { get; set; }

    /// <summary>並行權杖：畫面載入當下的值，存檔時比對是否已被他人改過。</summary>
    public Guid RowVersion { get; set; }

    public string DaAssetCode { get; set; } = "";
    public string DaAssetType { get; set; } = "";
    public string DaDescription { get; set; } = "";
    public string DaBackupMethod { get; set; } = "";
    public string DaRetentionPeriod { get; set; } = "";
    public string DaHasSensitiveData { get; set; } = "";
    public string DaUserUnit { get; set; } = "";
    public string DaRemark { get; set; } = "";
    public string DaBackupConfirm { get; set; } = "";
    public string DaReviewer { get; set; } = "";
    public string DaModifiedTime { get; set; } = "";
}

/// <summary>資訊系統、資料庫與檔案伺服器盤點表（系統盤點）區塊。</summary>
/// <remarks>
/// 對應 InfoSystem 其餘欄位，含編號／資產編號／資產名稱三個共用識別欄位——
/// 這三個只由本區塊維護，SW 與 DA 區塊不含它們，因此也不可能互相覆蓋。
/// </remarks>
[ModelMetadataType(typeof(InfoSystem))]
public class SystemEditViewModel : IInfoSystemBlockViewModel
{
    public int Id { get; set; }

    /// <summary>並行權杖：畫面載入當下的值，存檔時比對是否已被他人改過。</summary>
    public Guid RowVersion { get; set; }

    public string SeqNo { get; set; } = "";
    public string SystemCode { get; set; } = "";
    public string SystemName { get; set; } = "";
    public string Description { get; set; } = "";
    public string DbName { get; set; } = "";
    public string BackupLocation { get; set; } = "";
    public string BackupCycle { get; set; } = "";
    public string ExternalUnitName { get; set; } = "";
    public string HasLog { get; set; } = "";
    public string AccessCreate { get; set; } = "";
    public string AccessDelete { get; set; } = "";
    public string AccessCopy { get; set; } = "";
    public string FileDescription { get; set; } = "";
    public string SpecialData { get; set; } = "";
    public string SubjectCount { get; set; } = "";
    public string RetentionPeriod { get; set; } = "";
    public string Remark { get; set; } = "";
}

/// <summary>
/// 統一編輯畫面（EditAll）與統一新增畫面（CreateAll）的模型。
/// 一個畫面上有三個各自獨立送出的區塊，因此三個 ViewModel 併在這裡，
/// 各區塊的欄位在表單中以 Software. / Data. / Sheet3. 前綴區隔。
/// </summary>
public class InfoSystemEditViewModel
{
    /// <summary>
    /// 畫面標題、[檢視]連結等唯讀用途；新增時為空白實體。
    /// 標為不繫結也不驗證：它不是表單欄位，若讓模型繫結去驗證它，
    /// 實體上的 [Required] 會對著永遠空白的 Asset.SystemCode 報錯，
    /// 而畫面上沒有對應的輸入框，使用者會看到一個送不出去又找不到原因的表單。
    /// </summary>
    [BindNever, ValidateNever]
    public InfoSystem Asset { get; set; } = new();

    public SoftwareEditViewModel Software { get; set; } = new();
    public DataEditViewModel Data { get; set; } = new();
    public SystemEditViewModel Sheet3 { get; set; } = new();
}

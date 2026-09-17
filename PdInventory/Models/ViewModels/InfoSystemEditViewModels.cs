using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PdInventory.Models.ViewModels;

/// <summary>
/// SW／DA／系統盤點三個編輯區塊的 ViewModel，各自對應一個實體。
///
/// 為什麼不直接綁實體：那樣一次送出會綁定全部欄位，即使後續只寫回其中一部分，
/// 模型繫結階段仍然接受了不屬於這個畫面的輸入。改綁 ViewModel 之後，
/// 畫面能送什麼由型別本身決定，多送的欄位在繫結階段就被丟掉。
///
/// 欄位標題與驗證規則不在這裡重複宣告：[ModelMetadataType] 會讓 MVC 從實體
/// 取同名屬性的中繼資料，[Display] 仍然只有實體那一份。
///
/// 這三個類別要與各自的實體保持一致；漏掉欄位會在啟動時由
/// InfoSystemBlocks.AssertViewModelsCoverAllFields 直接擲出例外，不會安靜地存不進去。
/// </summary>
public interface IInfoSystemBlockViewModel
{
    int Id { get; set; }
    Guid RowVersion { get; set; }
}

/// <summary>資訊資產清單－軟體(SW)。含編號／資產編號／資產名稱／資產說明這幾個共用識別欄位，以及原本 SW 與 DA 各有一份、後來合併為 SW 那一份的欄位（資產狀態、機密性…）。</summary>
[ModelMetadataType(typeof(InfoSystem))]
public class SoftwareEditViewModel : IInfoSystemBlockViewModel
{
    public int Id { get; set; }

    /// <summary>並行權杖：畫面載入當下的值，存檔時比對是否已被他人改過。</summary>
    public Guid RowVersion { get; set; }

    public string SeqNo { get; set; } = "";
    public string SystemCode { get; set; } = "";
    public string SystemName { get; set; } = "";
    public string SwStatus { get; set; } = "";
    public string SwAssetType { get; set; } = "";
    public string SwSystemCategory { get; set; } = "";
    public string SwAdIntegration { get; set; } = "";
    public string SwDescription { get; set; } = "";
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
    public string SwReviewer { get; set; } = "";
}

/// <summary>資訊資產清單－資料(DA)。SystemCode 是關連的 SW 編號，可以留空。</summary>
[ModelMetadataType(typeof(DataAsset))]
public class DataEditViewModel : IInfoSystemBlockViewModel
{
    public int Id { get; set; }

    /// <summary>並行權杖：畫面載入當下的值，存檔時比對是否已被他人改過。</summary>
    public Guid RowVersion { get; set; }

    public string SystemCode { get; set; } = "";
    public string DaAssetCode { get; set; } = "";
    public string DaAssetType { get; set; } = "";
    public string DaDescription { get; set; } = "";
    public string DaBackupMethod { get; set; } = "";
    public string DaRetentionPeriod { get; set; } = "";
    public string DaHasSensitiveData { get; set; } = "";
    public string DaUserUnit { get; set; } = "";
    public string DaRemark { get; set; } = "";
    public string DaAdminNote { get; set; } = "";
    public string DaBackupConfirm { get; set; } = "";
    public string DaReviewer { get; set; } = "";
}

/// <summary>資訊系統、資料庫與檔案伺服器盤點表。SystemCode 必填，一定依附於某個軟體資產。</summary>
[ModelMetadataType(typeof(SystemInventory))]
public class SystemEditViewModel : IInfoSystemBlockViewModel
{
    public int Id { get; set; }

    /// <summary>並行權杖：畫面載入當下的值，存檔時比對是否已被他人改過。</summary>
    public Guid RowVersion { get; set; }

    /// <summary>
    /// 資產編號由控制器從所屬的 SW 帶入（<c>existing.SystemCode = system.SystemCode</c>），
    /// 畫面上那一格屬於 SW 區塊、不會以 Sheet3 前綴送出，因此這裡不驗證——
    /// 否則會被實體的[Required]擋下，整個盤點表區塊都存不了檔。
    /// </summary>
    [ValidateNever]
    public string SystemCode { get; set; } = "";

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
/// 三個區塊現在分屬三張資料表，DA 與盤點表可能還不存在，
/// 因此另外記下它們的主鍵，存檔時才知道要更新哪一列、還是要新建。
/// </summary>
public class InfoSystemEditViewModel
{
    /// <summary>畫面標題等唯讀用途；新增時、或這筆 DA 沒有關連 SW 時為空白實體。</summary>
    [BindNever, ValidateNever]
    public InfoSystem Asset { get; set; } = new();

    /// <summary>
    /// 沒有關連的軟體資產：SW 與盤點表兩個區塊在畫面上整塊不顯示，存檔也不會建那兩列。
    ///
    /// 新增畫面由使用者勾選「無SW資產編號」決定，因此這個屬性要能被繫結；
    /// 編輯畫面由 <see cref="AssetGroup.HasSoftware"/> 反推，那條路徑不繫結整個模型
    /// （三個區塊各自只繫結自己那一份 ViewModel），沒有被竄改的空間。
    /// </summary>
    public bool NoSoftwareAsset { get; set; }

    /// <summary>對應的 DataAssets 主鍵，也是統一編輯／檢視畫面的網址參數；新增時為 null。</summary>
    public int? DataAssetId { get; set; }

    /// <summary>對應的 SystemInventories 主鍵；null 表示還沒有盤點表資料。</summary>
    public int? InventoryId { get; set; }

    public SoftwareEditViewModel Software { get; set; } = new();
    public DataEditViewModel Data { get; set; } = new();
    public SystemEditViewModel Sheet3 { get; set; } = new();
}

/// <summary>個人資料檔案盤點表－人為產出。不含風險自評那一組欄位（由風險自評畫面維護），也不含軌跡與刪除註記。</summary>
[ModelMetadataType(typeof(InventoryItem))]
public class InventoryItemEditViewModel : IInfoSystemBlockViewModel
{
    public int Id { get; set; }

    /// <summary>並行權杖：畫面載入當下的值，存檔時比對是否已被他人改過。</summary>
    public Guid RowVersion { get; set; }

    public string SeqNo { get; set; } = "";
    public string DocumentName { get; set; } = "";
    public string SubjectCount { get; set; } = "";
    public string SubjectType { get; set; } = "";
    public bool HasSpecialData { get; set; }
    public string SpecialDataLegalBasis { get; set; } = "";
    public bool MinFieldCompliant { get; set; }
    public string SystemCode { get; set; } = "";
    public string SystemName { get; set; } = "";
    public string SourceCode { get; set; } = "";
    public string SourceName { get; set; } = "";
    public string CompanyRole { get; set; } = "";
    public string CollectProcedure { get; set; } = "";
    public string CollectStatement { get; set; } = "";
    public string CollectConsent { get; set; } = "";
    public string ProcessProcedure { get; set; } = "";
    public string ProcessDept { get; set; } = "";
    public string ProcessStatement { get; set; } = "";
    public string ProcessConsent { get; set; } = "";
    public string TransferTarget { get; set; } = "";
    public string TransferContract { get; set; } = "";
    public string TransferMethod { get; set; } = "";
    public string TransferCountry { get; set; } = "";
    public string RetentionPaper { get; set; } = "";
    public string RetentionDigital { get; set; } = "";
    public string LocationPaper { get; set; } = "";
    public string LocationDigital { get; set; } = "";
    public string Disposal { get; set; } = "";
    public string Remark { get; set; } = "";
}

/// <summary>系統自動拋轉(出/入)清單－系統產出。不含軌跡與刪除註記。</summary>
[ModelMetadataType(typeof(TransferRecord))]
public class TransferRecordEditViewModel : IInfoSystemBlockViewModel
{
    public int Id { get; set; }

    /// <summary>並行權杖：畫面載入當下的值，存檔時比對是否已被他人改過。</summary>
    public Guid RowVersion { get; set; }

    public string SeqNo { get; set; } = "";
    public string TransferType { get; set; } = "";
    public string SystemCode { get; set; } = "";
    public string SystemName { get; set; } = "";
    public string PathName { get; set; } = "";
    public string ExternalUnit { get; set; } = "";
    public string InternalUnit { get; set; } = "";
    public string ContentDescription { get; set; } = "";
    public string SpecialData { get; set; } = "";
    public string SubjectCount { get; set; } = "";
    public string InternationalTransfer { get; set; } = "";
    public string Contract { get; set; } = "";
    public string Remark { get; set; } = "";
}

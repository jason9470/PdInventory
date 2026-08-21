using System.ComponentModel.DataAnnotations;

namespace PdInventory.Models;

/// <summary>附表一：法務部公告個人資料類別（Sheet1「使用資料(欄位)」下拉維護資料）</summary>
public class PdCategory
{
    public int Id { get; set; }

    [Display(Name = "代號")]
    [Required(ErrorMessage = "代號必填"), StringLength(10)]
    public string Code { get; set; } = "";

    [Display(Name = "顯示代號")]
    [Required(ErrorMessage = "顯示代號必填"), StringLength(10)]
    public string DisplayCode { get; set; } = "";

    [Display(Name = "類別名稱")]
    [Required(ErrorMessage = "類別名稱必填"), StringLength(200)]
    public string Name { get; set; } = "";

    [Display(Name = "分類")]
    [StringLength(50)]
    public string GroupName { get; set; } = "";

    [Display(Name = "例示")]
    public string? Example { get; set; }

    public List<InventoryItem> InventoryItems { get; set; } = new();

    public string Label => $"{DisplayCode} {Name}";
}

/// <summary>附表二：法務部公告特定目的（Sheet1「特定目的」下拉維護資料）</summary>
public class Purpose
{
    public int Id { get; set; }

    [Display(Name = "代號")]
    [Required(ErrorMessage = "代號必填"), StringLength(10)]
    public string Code { get; set; } = "";

    [Display(Name = "特定目的名稱")]
    [Required(ErrorMessage = "名稱必填"), StringLength(300)]
    public string Name { get; set; } = "";

    public List<InventoryItem> InventoryItems { get; set; } = new();

    public string Label => $"{Code} {Name}";
}

/// <summary>Sheet1：個人資料檔案盤點表</summary>
public class InventoryItem
{
    public int Id { get; set; }

    // Ⅰ. 盤點基本資訊
    [Display(Name = "編號")]
    [Required(ErrorMessage = "編號必填"), StringLength(20)]
    public string SeqNo { get; set; } = "";

    [Display(Name = "含個人資料之文件/檔案/表單")]
    [Required(ErrorMessage = "文件/檔案/表單名稱必填"), StringLength(300)]
    public string DocumentName { get; set; } = "";

    // II. 個人資料檔案內容元素
    [Display(Name = "含個人資料當事人數量")]
    [StringLength(50)]
    public string SubjectCount { get; set; } = "";

    [Display(Name = "個資當事人類別")]
    [StringLength(100)]
    public string SubjectType { get; set; } = "";

    [Display(Name = "是否包含特種個資")]
    public bool HasSpecialData { get; set; }

    [Display(Name = "符合個資法§6條件(法源依據/公開來源)")]
    [StringLength(300)]
    public string SpecialDataLegalBasis { get; set; } = "";

    [Display(Name = "是否符合最小欄位要求")]
    public bool MinFieldCompliant { get; set; }

    [Display(Name = "資產編號")]
    [StringLength(10)]
    public string SystemCode { get; set; } = "";

    [Display(Name = "使用之資訊系統名稱")]
    [StringLength(200)]
    public string SystemName { get; set; } = "";

    [Display(Name = "來源代碼")]
    [StringLength(10)]
    public string SourceCode { get; set; } = "";

    [Display(Name = "來源對象/部門/資訊系統名稱")]
    [StringLength(200)]
    public string SourceName { get; set; } = "";

    [Display(Name = "公司角色")]
    [StringLength(50)]
    public string CompanyRole { get; set; } = "";

    // III. 個人資料生命循環 — 蒐集
    [Display(Name = "蒐集：程序文件名稱/使用行為描述/委外行為描述")]
    public string CollectProcedure { get; set; } = "";

    [Display(Name = "蒐集：個資聲明文件名稱/內容或法源依據")]
    public string CollectStatement { get; set; } = "";

    [Display(Name = "蒐集：當事人同意文件名稱/內容")]
    public string CollectConsent { get; set; } = "";

    // 處理
    [Display(Name = "處理：程序文件名稱/使用行為描述/委外行為描述")]
    public string ProcessProcedure { get; set; } = "";

    [Display(Name = "處理：傳送部門")]
    [StringLength(100)]
    public string ProcessDept { get; set; } = "";

    [Display(Name = "處理：個資聲明文件名稱/告知方式")]
    public string ProcessStatement { get; set; } = "";

    [Display(Name = "處理：當事人同意文件名稱")]
    public string ProcessConsent { get; set; } = "";

    // 傳輸
    [Display(Name = "傳輸：下一手傳遞對象/單位")]
    [StringLength(300)]
    public string TransferTarget { get; set; } = "";

    [Display(Name = "傳輸：委外合約/法源依據/傳送目的")]
    [StringLength(300)]
    public string TransferContract { get; set; } = "";

    [Display(Name = "傳輸：對外傳遞方式描述")]
    [StringLength(200)]
    public string TransferMethod { get; set; } = "";

    [Display(Name = "傳輸：國際傳輸國家名稱/城市名稱")]
    [StringLength(200)]
    public string TransferCountry { get; set; } = "";

    // 保存
    [Display(Name = "保存：保留期限(紙本)")]
    [StringLength(50)]
    public string RetentionPaper { get; set; } = "";

    [Display(Name = "保存：保留期限(電子)")]
    [StringLength(50)]
    public string RetentionDigital { get; set; } = "";

    [Display(Name = "保存：保存地點(紙本，地點/區域名稱)")]
    public string LocationPaper { get; set; } = "";

    [Display(Name = "保存：保存地點(電子，含音檔/圖檔等；資料夾名稱)")]
    public string LocationDigital { get; set; } = "";

    // 處置
    [Display(Name = "處置：期限屆滿後處置方式")]
    [StringLength(200)]
    public string Disposal { get; set; } = "";

    // IⅤ. 備註
    [Display(Name = "備註(個資使用項目明細)")]
    public string Remark { get; set; } = "";

    [Display(Name = "使用資料(欄位)")]
    public List<PdCategory> Categories { get; set; } = new();

    [Display(Name = "特定目的")]
    public List<Purpose> Purposes { get; set; } = new();

    // ───────────────────────────────────────────────────────────────
    // 個人資料風險自評表　以「資產編號」=SystemCode、「資產名稱」=SystemName、
    // 「個資文件/檔案名稱」=DocumentName 與本表對應（1 筆盤點項目對應 1 筆風險自評）
    // ───────────────────────────────────────────────────────────────
    [Display(Name = "風險-資料序號")]
    [StringLength(30)]
    public string RiskDataSeqNo { get; set; } = "";

    [Display(Name = "風險-風險分類編號")]
    [StringLength(20)]
    public string RiskCategoryCode { get; set; } = "";

    [Display(Name = "風險-風險描述分類")]
    [StringLength(100)]
    public string RiskCategoryName { get; set; } = "";

    [Display(Name = "風險-潛在風險事件")]
    public string RiskEvent { get; set; } = "";

    [Display(Name = "風險-影響程度評估")]
    [StringLength(30)]
    public string RiskImpactLevel { get; set; } = "";

    [Display(Name = "風險-發生可能性評估")]
    [StringLength(30)]
    public string RiskLikelihoodLevel { get; set; } = "";

    [Display(Name = "風險-相關規範(程序規章)")]
    [StringLength(300)]
    public string RiskRelatedRegulation { get; set; } = "";

    [Display(Name = "風險-現有控制點說明")]
    public string RiskControlDescription { get; set; } = "";

    [Display(Name = "風險-有效性等級")]
    [StringLength(30)]
    public string RiskEffectivenessLevel { get; set; } = "";

    [Display(Name = "風險-風險值")]
    [StringLength(20)]
    public string RiskValue { get; set; } = "";

    [Display(Name = "風險-改善措施或方案")]
    public string RiskImprovementPlan { get; set; } = "";

    [Display(Name = "風險-風險管理單位確認說明")]
    public string RiskUnitConfirm { get; set; } = "";

    [Display(Name = "風險-備註")]
    public string RiskRemark { get; set; } = "";
}

/// <summary>Sheet2：系統自動拋轉清單</summary>
public class TransferRecord
{
    public int Id { get; set; }

    [Display(Name = "編號")]
    [Required(ErrorMessage = "編號必填"), StringLength(20)]
    public string SeqNo { get; set; } = "";

    [Display(Name = "類別(拋入/拋出)")]
    [Required(ErrorMessage = "類別必填"), StringLength(20)]
    public string TransferType { get; set; } = "";

    [Display(Name = "資產編號")]
    [StringLength(10)]
    public string SystemCode { get; set; } = "";

    [Display(Name = "資產名稱")]
    [Required(ErrorMessage = "資產名稱必填"), StringLength(200)]
    public string SystemName { get; set; } = "";

    [Display(Name = "資料庫/資料夾名稱")]
    [StringLength(500)]
    public string PathName { get; set; } = "";

    [Display(Name = "拋轉來源/目的地之單位")]
    [StringLength(200)]
    public string ExternalUnit { get; set; } = "";

    [Display(Name = "負責內部單位")]
    [StringLength(100)]
    public string InternalUnit { get; set; } = "";

    [Display(Name = "拋轉檔案/報表名稱或內容描述")]
    public string ContentDescription { get; set; } = "";

    [Display(Name = "包含之特種個資內容")]
    [StringLength(200)]
    public string SpecialData { get; set; } = "";

    [Display(Name = "檔案含個人資料當事人數量")]
    [StringLength(50)]
    public string SubjectCount { get; set; } = "";

    [Display(Name = "國際傳輸國家名稱/城市名稱")]
    [StringLength(200)]
    public string InternationalTransfer { get; set; } = "";

    [Display(Name = "拋轉之委外合約/法源依據")]
    [StringLength(300)]
    public string Contract { get; set; } = "";

    [Display(Name = "備註")]
    public string Remark { get; set; } = "";
}

/// <summary>Sheet3：資訊系統、資料庫與檔案伺服器盤點表</summary>
public class InfoSystem
{
    public int Id { get; set; }

    [Display(Name = "編號")]
    [Required(ErrorMessage = "編號必填"), StringLength(20)]
    public string SeqNo { get; set; } = "";

    // I. 系統基本資訊
    [Display(Name = "資產編號")]
    [StringLength(10)]
    public string SystemCode { get; set; } = "";

    [Display(Name = "資產名稱")]
    [Required(ErrorMessage = "資產名稱必填"), StringLength(200)]
    public string SystemName { get; set; } = "";

    [Display(Name = "系統功能描述")]
    public string Description { get; set; } = "";

    [Display(Name = "資料庫/資料夾/檔案伺服器名稱")]
    public string DbName { get; set; } = "";

    // II. 備份
    [Display(Name = "備份地點")]
    [StringLength(300)]
    public string BackupLocation { get; set; } = "";

    [Display(Name = "備份週期")]
    [StringLength(200)]
    public string BackupCycle { get; set; } = "";

    // III. 外部單位存取權限盤點
    [Display(Name = "外部單位名稱")]
    [StringLength(200)]
    public string ExternalUnitName { get; set; } = "";

    [Display(Name = "是否有記錄操作紀錄或log")]
    [StringLength(20)]
    public string HasLog { get; set; } = "";

    [Display(Name = "存取權限：新增或修改")]
    [StringLength(50)]
    public string AccessCreate { get; set; } = "";

    [Display(Name = "存取權限：刪除")]
    [StringLength(50)]
    public string AccessDelete { get; set; } = "";

    [Display(Name = "存取權限：複製(下載、列印、可複製之查詢)")]
    [StringLength(50)]
    public string AccessCopy { get; set; } = "";

    [Display(Name = "檔案/報表名稱或內容描述")]
    public string FileDescription { get; set; } = "";

    [Display(Name = "包含之特種個資內容")]
    [StringLength(200)]
    public string SpecialData { get; set; } = "";

    [Display(Name = "檔案含個人資料當事人數量")]
    [StringLength(50)]
    public string SubjectCount { get; set; } = "";

    [Display(Name = "檔案保留期間")]
    [StringLength(100)]
    public string RetentionPeriod { get; set; } = "";

    // IV. 備註
    [Display(Name = "備註")]
    public string Remark { get; set; } = "";

    // ───────────────────────────────────────────────────────────────
    // 資訊資產清單－軟體(SW)　以資產編號(SW-xxx) = SystemCode 關聯本表
    // 資產編號=SystemCode、軟體資產名稱=SystemName、資產說明=Description（沿用既有欄位）
    // ───────────────────────────────────────────────────────────────
    [Display(Name = "SW-資產狀態")]
    [StringLength(20)]
    public string SwStatus { get; set; } = "";

    [Display(Name = "SW-資產類別")]
    [StringLength(20)]
    public string SwAssetType { get; set; } = "";

    [Display(Name = "SW-資產類別")]
    [StringLength(50)]
    public string SwSystemCategory { get; set; } = "";

    [Display(Name = "SW-與AD整合")]
    [StringLength(20)]
    public string SwAdIntegration { get; set; } = "";

    [Display(Name = "SW-作業系統/版本")]
    [StringLength(200)]
    public string SwOsVersion { get; set; } = "";

    [Display(Name = "SW-資料庫工具/版本")]
    [StringLength(200)]
    public string SwDbToolVersion { get; set; } = "";

    [Display(Name = "SW-第三方元件程式/版本")]
    public string SwThirdPartyComponents { get; set; } = "";

    [Display(Name = "SW-使用者帳號權限授與")]
    [StringLength(200)]
    public string SwUserAccountGrant { get; set; } = "";

    [Display(Name = "SW-是否提供帳號報表產出功能")]
    [StringLength(20)]
    public string SwProvidesAccountReport { get; set; } = "";

    [Display(Name = "SW-風險擁有者")]
    [StringLength(100)]
    public string SwRiskOwner { get; set; } = "";

    [Display(Name = "SW-位置")]
    [StringLength(200)]
    public string SwLocation { get; set; } = "";

    [Display(Name = "SW-權責單位")]
    [StringLength(100)]
    public string SwOwnerUnit { get; set; } = "";

    [Display(Name = "SW-保管單位")]
    [StringLength(100)]
    public string SwCustodianUnit { get; set; } = "";

    [Display(Name = "SW-使用單位")]
    [StringLength(200)]
    public string SwUserUnit { get; set; } = "";

    [Display(Name = "SW-機密性")]
    [StringLength(10)]
    public string SwConfidentiality { get; set; } = "";

    [Display(Name = "SW-完整性")]
    [StringLength(10)]
    public string SwIntegrity { get; set; } = "";

    [Display(Name = "SW-可用性")]
    [StringLength(10)]
    public string SwAvailability { get; set; } = "";

    [Display(Name = "SW-資產價值")]
    [StringLength(10)]
    public string SwAssetValue { get; set; } = "";

    [Display(Name = "SW-業務單位窗口")]
    [StringLength(100)]
    public string SwBusinessContact { get; set; } = "";

    [Display(Name = "SW-應用系統主管")]
    [StringLength(100)]
    public string SwAppManager { get; set; } = "";

    [Display(Name = "SW-應用系統維護人員")]
    [StringLength(100)]
    public string SwAppMaintainer { get; set; } = "";

    [Display(Name = "SW-應用系統維護代理人")]
    [StringLength(100)]
    public string SwAppMaintainerDeputy { get; set; } = "";

    [Display(Name = "SW-維運人員")]
    [StringLength(200)]
    public string SwOperator { get; set; } = "";

    [Display(Name = "SW-自行/委外開發")]
    [StringLength(50)]
    public string SwDevMode { get; set; } = "";

    [Display(Name = "SW-自行/委外維護")]
    [StringLength(50)]
    public string SwMaintMode { get; set; } = "";

    [Display(Name = "SW-委外廠商")]
    [StringLength(200)]
    public string SwVendor { get; set; } = "";

    [Display(Name = "SW-使用語言種類")]
    [StringLength(100)]
    public string SwLanguage { get; set; } = "";

    [Display(Name = "SW-版控系統")]
    [StringLength(100)]
    public string SwVersionControl { get; set; } = "";

    [Display(Name = "SW-AP版控專案目錄位址")]
    [StringLength(300)]
    public string SwApRepoPath { get; set; } = "";

    [Display(Name = "SW-上版方式")]
    [StringLength(100)]
    public string SwDeployMethod { get; set; } = "";

    [Display(Name = "SW-OP版控目錄位址")]
    [StringLength(300)]
    public string SwOpRepoPath { get; set; } = "";

    [Display(Name = "SW-程式碼存取方式")]
    [StringLength(200)]
    public string SwCodeAccess { get; set; } = "";

    [Display(Name = "SW-程式設計人員")]
    [StringLength(100)]
    public string SwDeveloper { get; set; } = "";

    [Display(Name = "SW-程式換版人員")]
    [StringLength(100)]
    public string SwDeployer { get; set; } = "";

    [Display(Name = "SW-回復資料目標(RPO)")]
    [StringLength(100)]
    public string SwRpo { get; set; } = "";

    [Display(Name = "SW-是否有同地備份")]
    [StringLength(20)]
    public string SwLocalBackup { get; set; } = "";

    [Display(Name = "SW-同地備份類型")]
    [StringLength(200)]
    public string SwLocalBackupType { get; set; } = "";

    [Display(Name = "SW-同地備份頻率")]
    [StringLength(200)]
    public string SwLocalBackupFreq { get; set; } = "";

    [Display(Name = "SW-是否有異地備份")]
    [StringLength(50)]
    public string SwRemoteBackup { get; set; } = "";

    [Display(Name = "SW-異地備份類型")]
    [StringLength(200)]
    public string SwRemoteBackupType { get; set; } = "";

    [Display(Name = "SW-異地備份頻率")]
    [StringLength(200)]
    public string SwRemoteBackupFreq { get; set; } = "";

    [Display(Name = "SW-是否有同地備援")]
    [StringLength(20)]
    public string SwLocalHa { get; set; } = "";

    [Display(Name = "SW-同地備援架構")]
    [StringLength(200)]
    public string SwLocalHaArch { get; set; } = "";

    [Display(Name = "SW-是否有異地備援")]
    [StringLength(20)]
    public string SwRemoteHa { get; set; } = "";

    [Display(Name = "SW-異地備援架構")]
    [StringLength(200)]
    public string SwRemoteHaArch { get; set; } = "";

    [Display(Name = "SW-回復時間目標(RTO)")]
    [StringLength(100)]
    public string SwRto { get; set; } = "";

    [Display(Name = "SW-有無備援與回復計畫")]
    [StringLength(20)]
    public string SwHasRecoveryPlan { get; set; } = "";

    [Display(Name = "SW-有無定期執行備援演練")]
    [StringLength(20)]
    public string SwHasDrDrill { get; set; } = "";

    [Display(Name = "SW-與哪些系統關聯性及影響程度高")]
    public string SwRelatedSystems { get; set; } = "";

    [Display(Name = "SW-備註")]
    public string SwRemark { get; set; } = "";

    [Display(Name = "SW-是否有處理個資")]
    [StringLength(20)]
    public string SwHandlesPersonalData { get; set; } = "";

    [Display(Name = "SW-是否具有使用者驗證及操作介面(UI)")]
    [StringLength(20)]
    public string SwHasUiAuth { get; set; } = "";

    [Display(Name = "SW-是否留存個資處理相關軌跡")]
    [StringLength(20)]
    public string SwKeepsPdTrail { get; set; } = "";

    [Display(Name = "SW-是否提供API")]
    [StringLength(20)]
    public string SwProvidesApi { get; set; } = "";

    [Display(Name = "SW-軌跡存放地點")]
    [StringLength(300)]
    public string SwTrailLocation { get; set; } = "";

    [Display(Name = "SW-軌跡留存方式")]
    [StringLength(100)]
    public string SwTrailStorage { get; set; } = "";

    [Display(Name = "SW-業務權責單位")]
    [StringLength(100)]
    public string SwBusinessOwnerUnit { get; set; } = "";

    [Display(Name = "SW-是否為核心系統")]
    [StringLength(20)]
    public string SwIsCoreSystem { get; set; } = "";

    [Display(Name = "SW-115上檢視人員")]
    [StringLength(100)]
    public string SwReviewer { get; set; } = "";

    [Display(Name = "SW-修改者")]
    [StringLength(100)]
    public string SwModifiedBy { get; set; } = "";

    [Display(Name = "SW-修改時間")]
    [StringLength(50)]
    public string SwModifiedTime { get; set; } = "";

    // ───────────────────────────────────────────────────────────────
    // 資訊資產清單－資料(DA)　以「關連SW編號」= SystemCode 關聯本表
    // ───────────────────────────────────────────────────────────────
    [Display(Name = "DA-資產編號")]
    [StringLength(20)]
    public string DaAssetCode { get; set; } = "";

    [Display(Name = "DA-資產類別")]
    [StringLength(20)]
    public string DaAssetType { get; set; } = "";

    [Display(Name = "DA-資產狀態")]
    [StringLength(20)]
    public string DaStatus { get; set; } = "";

    [Display(Name = "DA-資產說明")]
    public string DaDescription { get; set; } = "";

    [Display(Name = "DA-資料備份與保存方式")]
    [StringLength(300)]
    public string DaBackupMethod { get; set; } = "";

    [Display(Name = "DA-資料保留期限")]
    [StringLength(200)]
    public string DaRetentionPeriod { get; set; } = "";

    [Display(Name = "DA-有無機敏資料")]
    [StringLength(200)]
    public string DaHasSensitiveData { get; set; } = "";

    [Display(Name = "DA-風險擁有者")]
    [StringLength(100)]
    public string DaRiskOwner { get; set; } = "";

    [Display(Name = "DA-位置")]
    [StringLength(200)]
    public string DaLocation { get; set; } = "";

    [Display(Name = "DA-權責單位")]
    [StringLength(100)]
    public string DaOwnerUnit { get; set; } = "";

    [Display(Name = "DA-保管單位")]
    [StringLength(100)]
    public string DaCustodianUnit { get; set; } = "";

    [Display(Name = "DA-使用單位")]
    [StringLength(200)]
    public string DaUserUnit { get; set; } = "";

    [Display(Name = "DA-機密性")]
    [StringLength(10)]
    public string DaConfidentiality { get; set; } = "";

    [Display(Name = "DA-完整性")]
    [StringLength(10)]
    public string DaIntegrity { get; set; } = "";

    [Display(Name = "DA-可用性")]
    [StringLength(10)]
    public string DaAvailability { get; set; } = "";

    [Display(Name = "DA-資產價值")]
    [StringLength(10)]
    public string DaAssetValue { get; set; } = "";

    [Display(Name = "DA-備註")]
    public string DaRemark { get; set; } = "";

    [Display(Name = "DA-確認-資料備份與保存方式")]
    [StringLength(300)]
    public string DaBackupConfirm { get; set; } = "";

    [Display(Name = "DA-115檢視人員")]
    [StringLength(100)]
    public string DaReviewer { get; set; } = "";

    [Display(Name = "DA-修改時間")]
    [StringLength(50)]
    public string DaModifiedTime { get; set; } = "";
}

/// <summary>3-1：風險分類編號（風險自評「風險分類編號/風險描述分類/潛在風險事件」下拉維護資料）</summary>
public class RiskCategory
{
    public int Id { get; set; }

    [Display(Name = "風險分類編號")]
    [Required(ErrorMessage = "風險分類編號必填"), StringLength(20)]
    public string Code { get; set; } = "";

    [Display(Name = "風險描述分類")]
    [Required(ErrorMessage = "風險描述分類必填"), StringLength(100)]
    public string CategoryName { get; set; } = "";

    [Display(Name = "潛在風險事件描述")]
    public string EventDescription { get; set; } = "";

    [Display(Name = "參考之控制點")]
    public string ControlReference { get; set; } = "";

    public string Label => $"{Code} {CategoryName}";
}

/// <summary>3-2：評估影響程度（風險自評「影響程度評估」下拉維護資料）</summary>
public class RiskImpactLevel
{
    public int Id { get; set; }

    [Display(Name = "等級")]
    [Range(1, 5, ErrorMessage = "等級須為 1~5")]
    public int Level { get; set; }

    [Display(Name = "說明")]
    [Required(ErrorMessage = "說明必填"), StringLength(50)]
    public string Name { get; set; } = "";

    [Display(Name = "財務衝擊")]
    public string FinancialImpact { get; set; } = "";

    [Display(Name = "信譽損害")]
    public string ReputationImpact { get; set; } = "";

    [Display(Name = "個資當事人隱私衝擊")]
    public string PrivacyImpact { get; set; } = "";

    public string Label => $"{Level}：{Name}";
}

/// <summary>3-3：評估發生可能性（風險自評「發生可能性評估」下拉維護資料）</summary>
public class RiskLikelihoodLevel
{
    public int Id { get; set; }

    [Display(Name = "等級")]
    [Range(1, 5, ErrorMessage = "等級須為 1~5")]
    public int Level { get; set; }

    [Display(Name = "說明")]
    [Required(ErrorMessage = "說明必填"), StringLength(50)]
    public string Name { get; set; } = "";

    [Display(Name = "狀況描述")]
    public string Situation { get; set; } = "";

    [Display(Name = "發生頻率")]
    public string Frequency { get; set; } = "";

    public string Label => $"{Level}：{Name}";
}

/// <summary>3-4：有效性評估（風險自評「控制有效性等級」下拉維護資料）</summary>
public class RiskEffectivenessLevel
{
    public int Id { get; set; }

    [Display(Name = "等級")]
    [Range(1, 5, ErrorMessage = "等級須為 1~5")]
    public int Level { get; set; }

    [Display(Name = "說明")]
    [Required(ErrorMessage = "說明必填"), StringLength(50)]
    public string Name { get; set; } = "";

    [Display(Name = "有效性")]
    [StringLength(50)]
    public string Effectiveness { get; set; } = "";

    [Display(Name = "自行查核結果")]
    public string CheckResult { get; set; } = "";

    public string Label => $"{Level}：{Name}";
}

/// <summary>匯出 Excel 的欄位順序與是否納入（每張清單一組設定）。</summary>
public class ExportColumn
{
    public int Id { get; set; }

    /// <summary>清單代碼：Software / Data / Systems / Inventory / Risk / Transfers。</summary>
    [Required, StringLength(30)]
    public string ListKey { get; set; } = "";

    /// <summary>實體的屬性名稱。只存名稱不存標題，標題一律從 [Display] 取，維持單一來源。</summary>
    [Required, StringLength(100)]
    public string PropertyName { get; set; } = "";

    public int SortOrder { get; set; }

    /// <summary>是否納入匯出。</summary>
    public bool Included { get; set; } = true;
}

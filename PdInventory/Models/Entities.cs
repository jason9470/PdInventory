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

    [Display(Name = "使用之資訊系統名稱")]
    [StringLength(200)]
    public string SystemName { get; set; } = "";

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

    [Display(Name = "系統名稱")]
    [Required(ErrorMessage = "系統名稱必填"), StringLength(200)]
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
    [Display(Name = "系統名稱")]
    [Required(ErrorMessage = "系統名稱必填"), StringLength(200)]
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
}

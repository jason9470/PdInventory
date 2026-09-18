using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <summary>
    /// 匯入甲方 0918 提供的新增資料：系統產出 5 筆（編號 52～56）與盤點表 5 筆（SW-020、024、030、032、078）。
    /// 人為產出與自評表還在等甲方回覆（編號撞號、特定目的欄位內容、自評表要掛哪一列），這裡不處理。
    ///
    /// 每一筆都有條件，重跑不會重複建立：
    ///   系統產出——同一個編號已經存在就略過。
    ///   盤點表——那套系統已經有盤點表（一套系統一列）、或 SW 不存在就略過。
    ///
    /// 值由 scratchpad 的產生器直接從甲方的 txt 轉出，沒有手抄。轉檔時順手清掉兩個格式殘留：
    /// SW-020 備份週期裡的 HTML「&amp;nbsp;」，以及備份地點結尾多出來的一個引號。
    /// 盤點表檔案裡的「編號」「軟體資產名稱」「系統功能描述」三欄不存（盤點表沒有這些欄位，
    /// 名稱與說明在 SW），與 0902 匯入時的做法相同。
    /// </summary>
    public partial class ImportTransfersAndSystemInventories0918 : Migration
    {
        private const string Importer = "(0918 匯入)";

        private static readonly (string SeqNo, string TransferType, string SystemCode, string SystemName, string PathName, string ExternalUnit, string InternalUnit, string ContentDescription, string SpecialData, string SubjectCount, string InternationalTransfer, string Contract, string Remark)[] Transfers =
        [
            ("52", "拋入", "SW-030", "經紀系統(前台)", "各分公司前台主機", "交易所/櫃買中心", "作業中心", "B05-證券商之異常帳號成交資料作業\nB07-昨日市場普通違約公告檔\nB20-昨日市場信用違約公告檔\nB24-投資人開戶數異常表\nB35-證券商之投資人信用交易開戶數異常表\nB36-證券商之異常帳號成交資料作業\nB39-前日券商投資人連續五個營業日開戶達三戶以上者異常表\nB41-全權委託投資客戶開戶明細檔B51-投資人開戶明細查詢檔\nB53-信託戶開戶明細查詢檔B86-昨日期貨市場違約公告檔\nBC2-昨日市場普通違約公告檔", "N/A", "1-10", "N/A", "交易所開戶法規", "交易所透過主機連線方式，每日自動傳至AIX 主機分公司對應目錄。"),
            ("53", "拋出", "SW-030", "經紀系統(前台)", "各分公司前台主機", "交易所/櫃買中心", "作業中心", "BC6-現股當沖開戶申報作業", "N/A", "200-400", "N/A", "交易所開戶法規", "透過主機連線方式。如有集保，係屬人工方式產出920-開戶基本資料建檔媒體傳送檔，透過集保SMART系統上傳"),
            ("54", "拋出", "SW-030", "經紀系統(前台)", "各分公司前台主機", "交易所/櫃買中心", "作業中心", "B27-要求徵信資料", "N/A", "1-10", "N/A", "交易所開戶法規", "透過主機連線方式。如有集保，係屬人工方式產出920-開戶基本資料建檔媒體傳送檔，透過集保SMART系統上傳"),
            ("55", "拋出", "SW-030", "經紀系統(前台)", "各分公司前台主機", "交易所/櫃買中心", "作業中心", "開戶資料新增異動", "N/A", "500", "N/A", "交易所開戶法規", "透過主機連線方式。如有集保，係屬人工方式產出920-開戶基本資料建檔媒體傳送檔，透過集保SMART系統上傳"),
            ("56", "拋入", "SW-032", "複委託 OMS", "sftp://10.231.145.46/ibt/bank", "元大銀行", "國金部", "客戶銀行餘額 （台外幣)", "N/A", "500000", "N/A", "N/A", "銀行週一至週五轉入客戶銀行餘額 (和前次差異)"),
        ];

        private static readonly (string SystemCode, string DbName, string BackupLocation, string BackupCycle, string ExternalUnitName, string HasLog, string AccessCreate, string AccessDelete, string AccessCopy, string FileDescription, string SpecialData, string SubjectCount, string RetentionPeriod, string Remark)[] Inventories =
        [
            ("SW-020", "ecdb", "備份:Networker\n\nonline 異地備援: 板橋", "備份: 每天 異地備援 : 即時", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", ""),
            ("SW-024", "行情及IVR資料庫VTS_DB1 ( 172.20.68.75 )VTS_DB2 ( 172.20.68.76 )", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", ""),
            ("SW-030", "yuta55(10.215.2.55)\nyuta56(10.215.2.56)\nyuta57(10.215.2.57)\nyuta58(10.215.2.58)\nyuta59(10.216.2.59)\nyuta65(10.215.2.65)\nyuta66(10.215.2.66)\nyuta75(10.217.2.75)\nyuta76(10.217.2.76)\nyuta77(10.217.2.77)\nyuta78(10.217.2.78)\nyuta85(10.217.2.85)\nyuta86(10.217.2.86)\nyuta96(10.218.2.96)", "NetWorker 異地備份\n\n板橋機房/信義機房", "每天", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", ""),
            ("SW-032", "SBK 資料庫(FET1.TW.YUANTA.COM)", "NetWorker 異地備份(信義跟板橋機房)", "每天", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", ""),
            ("SW-078", "ORACLE: rs@ecdb", "Storage, 異地:板橋機房", "每天", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", ""),
        ];

        private static string Lit(string v) => "'" + v.Replace("'", "''") + "'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string now = "strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')";

            foreach (var r in Transfers)
            {
                migrationBuilder.Sql($"""
                    INSERT INTO "TransferRecords" ("SeqNo", "TransferType", "SystemCode", "SystemName", "PathName", "ExternalUnit", "InternalUnit", "ContentDescription", "SpecialData", "SubjectCount", "InternationalTransfer", "Contract", "Remark",
                        "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "RowVersion", "IsDeleted", "DeletedBy")
                    SELECT {Lit(r.SeqNo)}, {Lit(r.TransferType)}, {Lit(r.SystemCode)}, {Lit(r.SystemName)}, {Lit(r.PathName)}, {Lit(r.ExternalUnit)}, {Lit(r.InternalUnit)}, {Lit(r.ContentDescription)}, {Lit(r.SpecialData)}, {Lit(r.SubjectCount)}, {Lit(r.InternationalTransfer)}, {Lit(r.Contract)}, {Lit(r.Remark)},
                        '{Importer}', {now}, '{Importer}', {now}, '{Guid.NewGuid().ToString().ToUpperInvariant()}', 0, ''
                    WHERE NOT EXISTS (SELECT 1 FROM "TransferRecords" WHERE "SeqNo" = {Lit(r.SeqNo)} AND "IsDeleted" = 0);
                    """);
            }

            foreach (var r in Inventories)
            {
                migrationBuilder.Sql($"""
                    INSERT INTO "SystemInventories" ("SystemCode", "DbName", "BackupLocation", "BackupCycle", "ExternalUnitName", "HasLog", "AccessCreate", "AccessDelete", "AccessCopy", "FileDescription", "SpecialData", "SubjectCount", "RetentionPeriod", "Remark",
                        "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "RowVersion", "IsDeleted", "DeletedBy")
                    SELECT {Lit(r.SystemCode)}, {Lit(r.DbName)}, {Lit(r.BackupLocation)}, {Lit(r.BackupCycle)}, {Lit(r.ExternalUnitName)}, {Lit(r.HasLog)}, {Lit(r.AccessCreate)}, {Lit(r.AccessDelete)}, {Lit(r.AccessCopy)}, {Lit(r.FileDescription)}, {Lit(r.SpecialData)}, {Lit(r.SubjectCount)}, {Lit(r.RetentionPeriod)}, {Lit(r.Remark)},
                        '{Importer}', {now}, '{Importer}', {now}, '{Guid.NewGuid().ToString().ToUpperInvariant()}', 0, ''
                    WHERE EXISTS (SELECT 1 FROM "InfoSystems" WHERE "SystemCode" = {Lit(r.SystemCode)} AND "IsDeleted" = 0)
                      AND NOT EXISTS (SELECT 1 FROM "SystemInventories" WHERE "SystemCode" = {Lit(r.SystemCode)} AND "IsDeleted" = 0);
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 只刪這次匯入、而且之後沒有人改過的列（修改者仍是匯入標記）
            migrationBuilder.Sql($"""DELETE FROM "TransferRecords" WHERE "CreatedBy" = '{Importer}' AND "UpdatedBy" = '{Importer}';""");
            migrationBuilder.Sql($"""DELETE FROM "SystemInventories" WHERE "CreatedBy" = '{Importer}' AND "UpdatedBy" = '{Importer}';""");
        }
    }
}

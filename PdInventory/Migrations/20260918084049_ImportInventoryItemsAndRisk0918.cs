using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <summary>
    /// 匯入甲方 0918 第二版的人為產出 3 筆與對應的自評表 3 筆（自評表存在人為產出同一列上）。
    ///
    /// 編號接續現有（業務端 0918 決定）：檔案上的人為產出 19～21 → 22～24、
    /// 自評表風險序號 18～20 → 21～23，對應關係不變：
    ///   BC6-現股當沖開戶申報作業 22 / 21、B27-要求徵信資料 23 / 22、B50-開戶資料新增異動 24 / 23。
    ///
    /// 轉檔規則與 0902 匯入相同：「SW-030 經紀系統(前台)」拆成資產編號與名稱；
    /// 使用資料（COO一、ＣOO三…）轉成個資類別代碼、特定目的取三碼代碼，都以代碼對到維護表，不寫死主鍵；
    /// 有效性等級補成「2：良好」這種寫法。檔案上系統沒有對應欄位的幾欄不存：
    /// 人為產出的「利用－程序文件名稱」（內容都是 N/A）、自評表的「組別」「114檢視者員編」「修改時間」。
    ///
    /// 每一筆都有條件：同一個編號已經存在、或同一個資產已經有同名文件就略過，重跑不會重複建立。
    /// 值由 scratchpad 的產生器直接從甲方的 txt 轉出，沒有手抄。
    /// </summary>
    public partial class ImportInventoryItemsAndRisk0918 : Migration
    {
        private const string Importer = "(0918 匯入)";

        private record Row(Dictionary<string, string> Values, string[] Categories, string[] Purposes);

        private static readonly Row[] Rows =
        [
            new(new Dictionary<string, string>
            {
                ["SeqNo"] = "22",
                ["DocumentName"] = "BC6-現股當沖開戶申報作業",
                ["SubjectCount"] = "2",
                ["SubjectType"] = "客戶",
                ["HasSpecialData"] = "0",
                ["SpecialDataLegalBasis"] = "Y",
                ["MinFieldCompliant"] = "1",
                ["SystemCode"] = "SW-030",
                ["SystemName"] = "經紀系統(前台)",
                ["SourceCode"] = "SW-027",
                ["SourceName"] = "AMS 帳戶系統",
                ["CompanyRole"] = "資料控制者",
                ["CollectProcedure"] = "證券商現股當沖開戶申報檔",
                ["CollectStatement"] = "無，資料由 AMS 提供",
                ["CollectConsent"] = "無，資料由 AMS 提供",
                ["ProcessProcedure"] = "證券商現股當沖開戶申報檔",
                ["ProcessDept"] = "N/A",
                ["ProcessStatement"] = "無，資料由 AMS 提供",
                ["ProcessConsent"] = "無，資料由 AMS 提供",
                ["TransferTarget"] = "台灣證券交易所，\n證券櫃檯買賣中心",
                ["TransferContract"] = "",
                ["TransferMethod"] = "電子交換",
                ["TransferCountry"] = "",
                ["RetentionPaper"] = "",
                ["RetentionDigital"] = "永久保存",
                ["LocationPaper"] = "",
                ["LocationDigital"] = "資料備份系統",
                ["Disposal"] = "無",
                ["Remark"] = "券商代號, 客戶帳號, 身份證字號",
                ["RiskDataSeqNo"] = "21",
                ["RiskCategoryCode"] = "RETE001",
                ["RiskCategoryName"] = "個人資料風險 - 外部傳遞(系統/電子檔)",
                ["RiskEvent"] = "透過經紀系統(前台)將BC6-現股當沖開戶申報作業傳遞予交易所/櫃買中心時，未適當考量保護措施，致使個資遺失/外洩",
                ["RiskImpactLevel"] = "2：中等",
                ["RiskLikelihoodLevel"] = "1：不太可能發生",
                ["RiskRelatedRegulation"] = "主機連線作業手冊(證交所)",
                ["RiskControlDescription"] = "專線傳輸",
                ["RiskEffectivenessLevel"] = "2：良好",
                ["RiskValue"] = "4",
                ["RiskImprovementPlan"] = "",
                ["RiskUnitConfirm"] = "",
                ["RiskRemark"] = ""
            },
            ["C001", "C003"],
            ["061", "069"]),
            new(new Dictionary<string, string>
            {
                ["SeqNo"] = "23",
                ["DocumentName"] = "B27-要求徵信資料",
                ["SubjectCount"] = "1/1/1",
                ["SubjectType"] = "客戶",
                ["HasSpecialData"] = "0",
                ["SpecialDataLegalBasis"] = "Y",
                ["MinFieldCompliant"] = "1",
                ["SystemCode"] = "SW-030",
                ["SystemName"] = "經紀系統(前台)",
                ["SourceCode"] = "SW-030",
                ["SourceName"] = "經紀系統(前台)",
                ["CompanyRole"] = "資料控制者",
                ["CollectProcedure"] = "證券經紀聯合徵信檔",
                ["CollectStatement"] = "無，公司徵信資料用",
                ["CollectConsent"] = "無，公司徵信資料用",
                ["ProcessProcedure"] = "證券經紀聯合徵信檔",
                ["ProcessDept"] = "N/A",
                ["ProcessStatement"] = "無，公司徵信資料用",
                ["ProcessConsent"] = "無，公司徵信資料用",
                ["TransferTarget"] = "台灣證券交易所，\n證券櫃檯買賣中心",
                ["TransferContract"] = "",
                ["TransferMethod"] = "電子交換",
                ["TransferCountry"] = "",
                ["RetentionPaper"] = "",
                ["RetentionDigital"] = "永久保存",
                ["LocationPaper"] = "",
                ["LocationDigital"] = "資料備份系統",
                ["Disposal"] = "無",
                ["Remark"] = "身份證字號, 姓名",
                ["RiskDataSeqNo"] = "22",
                ["RiskCategoryCode"] = "RETE001",
                ["RiskCategoryName"] = "個人資料風險 - 外部傳遞(系統/電子檔)",
                ["RiskEvent"] = "透過經紀系統(前台)將B27-要求徵信資料傳遞予交易所/櫃買中心時，未適當考量保護措施，致使個資遺失/外洩",
                ["RiskImpactLevel"] = "2：中等",
                ["RiskLikelihoodLevel"] = "1：不太可能發生",
                ["RiskRelatedRegulation"] = "主機連線作業手冊(證交所)",
                ["RiskControlDescription"] = "專線傳輸",
                ["RiskEffectivenessLevel"] = "2：良好",
                ["RiskValue"] = "4",
                ["RiskImprovementPlan"] = "",
                ["RiskUnitConfirm"] = "",
                ["RiskRemark"] = ""
            },
            ["C001", "C003"],
            ["061", "069"]),
            new(new Dictionary<string, string>
            {
                ["SeqNo"] = "24",
                ["DocumentName"] = "B50-開戶資料新增異動",
                ["SubjectCount"] = "16",
                ["SubjectType"] = "客戶",
                ["HasSpecialData"] = "0",
                ["SpecialDataLegalBasis"] = "Y",
                ["MinFieldCompliant"] = "1",
                ["SystemCode"] = "SW-030",
                ["SystemName"] = "經紀系統(前台)",
                ["SourceCode"] = "SW-027",
                ["SourceName"] = "AMS 帳戶系統",
                ["CompanyRole"] = "資料控制者",
                ["CollectProcedure"] = "證券客戶開戶資料檔",
                ["CollectStatement"] = "無，資料由 AMS 提供",
                ["CollectConsent"] = "無，資料由 AMS 提供",
                ["ProcessProcedure"] = "證券客戶開戶資料檔",
                ["ProcessDept"] = "N/A",
                ["ProcessStatement"] = "無，資料由 AMS 提供",
                ["ProcessConsent"] = "無，資料由 AMS 提供",
                ["TransferTarget"] = "台灣證券交易所，\n證券櫃檯買賣中心",
                ["TransferContract"] = "",
                ["TransferMethod"] = "電子交換",
                ["TransferCountry"] = "",
                ["RetentionPaper"] = "",
                ["RetentionDigital"] = "永久保存",
                ["LocationPaper"] = "",
                ["LocationDigital"] = "資料備份系統",
                ["Disposal"] = "無",
                ["Remark"] = "券商代號, 客戶帳號, 信用帳號, 身份證字號, 客戶姓名, 身份別, 出生日期, 銀行帳號, 集中客戶狀態, 櫃買客戶狀態, 法定代理人",
                ["RiskDataSeqNo"] = "23",
                ["RiskCategoryCode"] = "RETE001",
                ["RiskCategoryName"] = "個人資料風險 - 外部傳遞(系統/電子檔)",
                ["RiskEvent"] = "透過經紀系統(前台)將B50-開戶資料新增異動傳遞予交易所/櫃買中心時，未適當考量保護措施，致使個資遺失/外洩",
                ["RiskImpactLevel"] = "3：顯著",
                ["RiskLikelihoodLevel"] = "1：不太可能發生",
                ["RiskRelatedRegulation"] = "主機連線作業手冊(證交所)",
                ["RiskControlDescription"] = "專線傳輸",
                ["RiskEffectivenessLevel"] = "2：良好",
                ["RiskValue"] = "6",
                ["RiskImprovementPlan"] = "",
                ["RiskUnitConfirm"] = "",
                ["RiskRemark"] = ""
            },
            ["C001", "C003", "C011", "C031"],
            ["061", "069"]),
        ];

        private static readonly string[] Columns = ["SeqNo", "DocumentName", "SubjectCount", "SubjectType", "HasSpecialData", "SpecialDataLegalBasis", "MinFieldCompliant", "SystemCode", "SystemName", "SourceCode", "SourceName", "CompanyRole", "CollectProcedure", "CollectStatement", "CollectConsent", "ProcessProcedure", "ProcessDept", "ProcessStatement", "ProcessConsent", "TransferTarget", "TransferContract", "TransferMethod", "TransferCountry", "RetentionPaper", "RetentionDigital", "LocationPaper", "LocationDigital", "Disposal", "Remark", "RiskDataSeqNo", "RiskCategoryCode", "RiskCategoryName", "RiskEvent", "RiskImpactLevel", "RiskLikelihoodLevel", "RiskRelatedRegulation", "RiskControlDescription", "RiskEffectivenessLevel", "RiskValue", "RiskImprovementPlan", "RiskUnitConfirm", "RiskRemark"];

        /// <summary>這兩欄是布林，存 0／1，不加引號。</summary>
        private static readonly HashSet<string> Booleans = ["HasSpecialData", "MinFieldCompliant"];

        private static string Lit(string v) => "'" + v.Replace("'", "''") + "'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string now = "strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')";

            foreach (var row in Rows)
            {
                var v = row.Values;
                var values = string.Join(", ", Columns.Select(c => Booleans.Contains(c) ? v[c] : Lit(v[c])));

                migrationBuilder.Sql($"""
                    INSERT INTO "InventoryItems" ("SeqNo", "DocumentName", "SubjectCount", "SubjectType", "HasSpecialData", "SpecialDataLegalBasis", "MinFieldCompliant", "SystemCode", "SystemName", "SourceCode", "SourceName", "CompanyRole", "CollectProcedure", "CollectStatement", "CollectConsent", "ProcessProcedure", "ProcessDept", "ProcessStatement", "ProcessConsent", "TransferTarget", "TransferContract", "TransferMethod", "TransferCountry", "RetentionPaper", "RetentionDigital", "LocationPaper", "LocationDigital", "Disposal", "Remark", "RiskDataSeqNo", "RiskCategoryCode", "RiskCategoryName", "RiskEvent", "RiskImpactLevel", "RiskLikelihoodLevel", "RiskRelatedRegulation", "RiskControlDescription", "RiskEffectivenessLevel", "RiskValue", "RiskImprovementPlan", "RiskUnitConfirm", "RiskRemark",
                        "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "RowVersion", "IsDeleted", "DeletedBy")
                    SELECT {values},
                        '{Importer}', {now}, '{Importer}', {now}, '{Guid.NewGuid().ToString().ToUpperInvariant()}', 0, ''
                    WHERE NOT EXISTS (SELECT 1 FROM "InventoryItems" WHERE "SeqNo" = {Lit(v["SeqNo"])} AND "IsDeleted" = 0)
                      AND NOT EXISTS (SELECT 1 FROM "InventoryItems" WHERE "SystemCode" = {Lit(v["SystemCode"])}
                                      AND trim("DocumentName") = {Lit(v["DocumentName"])} AND "IsDeleted" = 0);
                    """);

                // 多對多以代碼對到維護表；只接這次匯入的那一列（編號＋匯入標記），不碰同編號的其他資料
                var item = $"""SELECT "Id" FROM "InventoryItems" WHERE "SeqNo" = {Lit(v["SeqNo"])} AND "CreatedBy" = '{Importer}' AND "IsDeleted" = 0""";

                migrationBuilder.Sql($"""
                    INSERT INTO "InventoryItemCategories" ("CategoriesId", "InventoryItemsId")
                    SELECT c."Id", i."Id" FROM "Categories" c, ({item}) i
                    WHERE c."Code" IN ({string.Join(", ", row.Categories.Select(Lit))})
                      AND NOT EXISTS (SELECT 1 FROM "InventoryItemCategories" x
                                      WHERE x."CategoriesId" = c."Id" AND x."InventoryItemsId" = i."Id");
                    """);

                migrationBuilder.Sql($"""
                    INSERT INTO "InventoryItemPurposes" ("InventoryItemsId", "PurposesId")
                    SELECT i."Id", p."Id" FROM "Purposes" p, ({item}) i
                    WHERE p."Code" IN ({string.Join(", ", row.Purposes.Select(Lit))})
                      AND NOT EXISTS (SELECT 1 FROM "InventoryItemPurposes" x
                                      WHERE x."PurposesId" = p."Id" AND x."InventoryItemsId" = i."Id");
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 只刪這次匯入、而且之後沒有人改過的列（修改者仍是匯入標記）；多對多的連結跟著刪
            const string mine = """SELECT "Id" FROM "InventoryItems" WHERE "CreatedBy" = '(0918 匯入)' AND "UpdatedBy" = '(0918 匯入)'""";
            migrationBuilder.Sql($"""DELETE FROM "InventoryItemCategories" WHERE "InventoryItemsId" IN ({mine});""");
            migrationBuilder.Sql($"""DELETE FROM "InventoryItemPurposes" WHERE "InventoryItemsId" IN ({mine});""");
            migrationBuilder.Sql($"""DELETE FROM "InventoryItems" WHERE "Id" IN ({mine});""");
        }
    }
}

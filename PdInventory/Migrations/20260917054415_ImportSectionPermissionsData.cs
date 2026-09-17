using Microsoft.EntityFrameworkCore.Migrations;
using PdInventory.Models;

#nullable disable

namespace PdInventory.Migrations
{
    /// <summary>
    /// 權限改以科別為中心（0917）的<b>資料</b>部分：依業務端提供的「權限管理檔」與「人員檔」
    /// 建立科別、各科負責的系統、人員的科別，並依職稱調整角色。結構在前一個 migration。
    ///
    /// 每一句都有條件，在已經有這些資料的環境上重跑不會重複建立，也不會覆蓋別人改過的值。
    /// </summary>
    public partial class ImportSectionPermissionsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 以下資料來自業務端 0917 提供的兩份對照檔：「權限管理檔」與「人員檔」────────
            // 由 scratchpad 的產生器從原始檔直接轉出，沒有手抄。
            SeedSections(migrationBuilder);
            SeedSectionSystems(migrationBuilder);
            AssignEmployees(migrationBuilder);
            RaiseRoles(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 清掉匯入的對照與人員科別；角色調高的部分不還原（不知道原本是什麼）
            migrationBuilder.Sql("""UPDATE "Employees" SET "SectionId" = NULL;""");
            migrationBuilder.Sql("""DELETE FROM "SectionSystems";""");
            migrationBuilder.Sql("""DELETE FROM "Sections";""");
        }

        /// <summary>科別：部室 2、組 6、科 14。排序值讓「一二三四五六」照數字排。</summary>
        private static readonly (string Name, string Team, SectionKind Kind, int Order)[] Sections =
        [
            ("部室主管", "部室主管", SectionKind.Office, 1),
            ("部室副主管", "部室副主管", SectionKind.Office, 2),
            ("開發一組", "開發一組", SectionKind.Team, 100),
            ("通路核心科", "開發一組", SectionKind.Section, 101),
            ("前台核心科", "開發一組", SectionKind.Section, 102),
            ("開發二組", "開發二組", SectionKind.Team, 200),
            ("經紀帳務科", "開發二組", SectionKind.Section, 201),
            ("帳務服務科", "開發二組", SectionKind.Section, 202),
            ("開發三組", "開發三組", SectionKind.Team, 300),
            ("通路整合科", "開發三組", SectionKind.Section, 301),
            ("API通路服務科", "開發三組", SectionKind.Section, 302),
            ("創新研發科", "開發三組", SectionKind.Section, 303),
            ("開發四組", "開發四組", SectionKind.Team, 400),
            ("應用服務科", "開發四組", SectionKind.Section, 401),
            ("股務代理科", "開發四組", SectionKind.Section, 402),
            ("開發五組", "開發五組", SectionKind.Team, 500),
            ("大財管開發科", "開發五組", SectionKind.Section, 501),
            ("帳戶服務科", "開發五組", SectionKind.Section, 502),
            ("ERP科", "開發五組", SectionKind.Section, 503),
            ("開發六組", "開發六組", SectionKind.Team, 600),
            ("營運管理科", "開發六組", SectionKind.Section, 601),
            ("投資管理科", "開發六組", SectionKind.Section, 602),
        ];

        /// <summary>
        /// 各科負責的系統。組不在這裡：組的範圍一律是底下各科的聯集（見 SectionScope），
        /// 業務端檔案裡每個組那一列也剛好都等於聯集。
        /// SW-118 同時屬於營運管理科與投資管理科。
        /// </summary>
        private static readonly (string Section, string[] Codes)[] SectionSystemCodes =
        [
            ("通路核心科", ["SW-020", "SW-024", "SW-026"]),
            ("前台核心科", ["SW-030", "SW-032", "SW-078", "SW-019"]),
            ("經紀帳務科", ["SW-108", "SW-041"]),
            ("帳務服務科", ["SW-021", "SW-036", "SW-046", "SW-034"]),
            ("通路整合科", ["SW-003", "SW-011", "SW-012", "SW-117", "SW-129", "SW-133"]),
            ("API通路服務科", ["SW-004", "SW-010"]),
            ("創新研發科", ["SW-008", "SW-016", "SW-017", "SW-023", "SW-025", "SW-131", "SW-153"]),
            ("應用服務科", ["SW-042", "SW-075", "SW-076", "SW-096", "SW-106", "SW-141"]),
            ("股務代理科", ["SW-074"]),
            ("大財管開發科", ["SW-029", "SW-033", "SW-134", "SW-135"]),
            ("帳戶服務科", ["SW-027", "SW-104"]),
            ("ERP科", ["SW-098", "SW-132", "SW-154"]),
            ("營運管理科", ["SW-118"]),
            ("投資管理科", ["SW-082", "SW-083", "SW-084", "SW-086", "SW-105", "SW-114", "SW-118"]),
        ];

        /// <summary>人員的科別與職稱（註）。員編已補成七碼。</summary>
        private static readonly (string EmpNo, string Section, string Note)[] EmployeeSections =
        [
            ("0007331", "部室主管", "管理員"),  // 陳映玲
            ("0005784", "部室副主管", "管理員"),  // 許稚苓
            ("0006346", "開發一組", "組長"),  // 張瑜玫
            ("0013706", "通路核心科", "科長"),  // 周佩君
            ("0183120", "通路核心科", ""),  // 倪柏寧
            ("0183426", "通路核心科", ""),  // 林銘偉
            ("0181039", "通路核心科", ""),  // 王宏志
            ("0002876", "前台核心科", ""),  // 謝江河
            ("0013797", "前台核心科", ""),  // 范維德
            ("0015114", "前台核心科", "科長"),  // 王敏耀
            ("0006352", "前台核心科", ""),  // 蘇明暉
            ("0015123", "前台核心科", ""),  // 張淑娟
            ("0180889", "前台核心科", ""),  // 張書瑋
            ("0181188", "前台核心科", ""),  // 吳旻峰
            ("0180366", "前台核心科", ""),  // 劉駿
            ("0182552", "前台核心科", ""),  // 吳浚瑋
            ("0050305", "前台核心科", ""),  // 曾政嘉
            ("0050323", "前台核心科", ""),  // 柯皓中
            ("0182938", "前台核心科", ""),  // 張家豪
            ("0012033", "開發二組", "組長"),  // 紀韶維
            ("0013870", "經紀帳務科", ""),  // 鍾春懿
            ("0012081", "經紀帳務科", "科長"),  // 張瑛瑛
            ("0014770", "經紀帳務科", ""),  // 高志豪
            ("0015113", "經紀帳務科", ""),  // 張淑美
            ("0014051", "經紀帳務科", ""),  // 王從道
            ("0014412", "經紀帳務科", ""),  // 許文瀚
            ("0181957", "經紀帳務科", ""),  // 郭悅瑜
            ("0014834", "經紀帳務科", ""),  // 鄭瑋霖
            ("0050326", "經紀帳務科", ""),  // 鍾宏偉
            ("0020906", "帳務服務科", "科長"),  // 楊常鑫
            ("0006340", "帳務服務科", ""),  // 吳淑芳
            ("0006890", "帳務服務科", ""),  // 黃桂芳
            ("0015118", "帳務服務科", ""),  // 林惠蘭
            ("0006341", "經紀帳務科", "管理員"),  // 黃美倫
            ("0183253", "帳務服務科", ""),  // 林子耕
            ("0183307", "經紀帳務科", ""),  // 許庭溦
            ("0014078", "經紀帳務科", ""),  // 黃鈺棠
            ("0005837", "開發三組", "組長"),  // 邱慶霖
            ("0013729", "API通路服務科", "科長"),  // 邱永嬌
            ("0101046", "API通路服務科", ""),  // 柳怡禎
            ("0013776", "API通路服務科", ""),  // 粘淑婷
            ("0014352", "API通路服務科", ""),  // 翁群弼
            ("0181852", "API通路服務科", ""),  // 柯亞慧
            ("0182121", "API通路服務科", ""),  // 陳信宏
            ("0183531", "API通路服務科", ""),  // 王俞之
            ("0183385", "API通路服務科", ""),  // 高漢庭
            ("0021111", "通路整合科", ""),  // 林志成
            ("0180212", "通路整合科", "科長"),  // 施凱家
            ("0013681", "通路整合科", ""),  // 邱柏翰
            ("0013775", "通路整合科", ""),  // 許暉煌
            ("0181014", "通路整合科", ""),  // 林士恒
            ("0021758", "通路整合科", ""),  // 張宏文
            ("0183401", "通路整合科", ""),  // 陳家正
            ("0182043", "通路整合科", ""),  // 郭哲佑
            ("0183952", "通路整合科", ""),  // 蔡昀霖
            ("0014486", "創新研發科", "科長"),  // 林大鈞
            ("0007975", "創新研發科", ""),  // 王怡如
            ("0011373", "創新研發科", ""),  // 陳瑞雄
            ("0013748", "創新研發科", ""),  // 陳佳駿
            ("0014969", "創新研發科", ""),  // 穆皆仰
            ("0021604", "創新研發科", ""),  // 陳佩伶
            ("0020931", "創新研發科", ""),  // 陳倉億
            ("0180330", "創新研發科", ""),  // 江明哲
            ("0180352", "創新研發科", ""),  // 陳奕凱
            ("0021142", "創新研發科", ""),  // 黃朝聰
            ("0181495", "創新研發科", ""),  // 洪梓寧
            ("0182430", "創新研發科", ""),  // 高宗毅
            ("0182554", "創新研發科", ""),  // 林軒宇
            ("0182570", "創新研發科", ""),  // 郭晉晏
            ("0183968", "創新研發科", ""),  // 林冠汝
            ("0015138", "開發五組", "組長"),  // 洪才元
            ("0050082", "大財管開發科", "科長"),  // 蔡嘉駒
            ("0010196", "大財管開發科", ""),  // 彭先達
            ("0021567", "大財管開發科", ""),  // 薛先㨗
            ("0013108", "大財管開發科", ""),  // 陳佳淇
            ("0013291", "大財管開發科", ""),  // 蔡文斌
            ("0014207", "大財管開發科", ""),  // 廖俊華
            ("0014706", "大財管開發科", ""),  // 林昕禾
            ("0014057", "大財管開發科", ""),  // 陳冠州
            ("0181834", "大財管開發科", ""),  // 許閔智
            ("0181821", "大財管開發科", ""),  // 李品諺
            ("0182462", "大財管開發科", ""),  // 吳怡萱
            ("0013955", "帳戶服務科", "科長"),  // 陳逸群
            ("0014509", "帳戶服務科", ""),  // 羅郁仁
            ("0181492", "帳戶服務科", ""),  // 黃緯珣
            ("0180071", "帳戶服務科", ""),  // 彭紹旻
            ("0181918", "帳戶服務科", ""),  // 林學孜
            ("0182320", "帳戶服務科", ""),  // 戴翊竹
            ("0182472", "帳戶服務科", ""),  // 何彥儀
            ("0182804", "帳戶服務科", ""),  // 張永奕
            ("0012461", "ERP科", "科長"),  // 張永顯
            ("0012811", "ERP科", ""),  // 張睿智
            ("0064620", "ERP科", ""),  // 洪睿嬪
            ("0080620", "ERP科", ""),  // 鍾淑暖
            ("0021605", "ERP科", ""),  // 蔡德華
            ("0002747", "ERP科", ""),  // 曾幸惠
            ("0183079", "帳戶服務科", ""),  // 鄭凱倫
            ("0183440", "大財管開發科", ""),  // 趙妤瑄
            ("0183713", "大財管開發科", ""),  // 陳宥鈊
            ("0010743", "營運管理科", "科長"),  // 林沛樺
            ("0020767", "營運管理科", ""),  // 姚曉華
            ("0014465", "營運管理科", ""),  // 孫大川
            ("0180709", "營運管理科", ""),  // 劉庭妤
            ("0011387", "開發六組", "組長"),  // 王敏娟
            ("0012842", "投資管理科", "科長"),  // 張容禎
            ("0012057", "投資管理科", ""),  // 蕭育智
            ("0021189", "投資管理科", ""),  // 楊捷扉
            ("0014210", "投資管理科", ""),  // 謝錫宗
            ("0021635", "投資管理科", ""),  // 張雅惠
            ("0013998", "投資管理科", ""),  // 黃耀賢
            ("0014350", "投資管理科", ""),  // 朱怡芬
            ("0180702", "投資管理科", ""),  // 呂盈暄
            ("0180672", "投資管理科", ""),  // 賴志傑
            ("0182042", "投資管理科", ""),  // 林浩存
            ("0182291", "投資管理科", ""),  // 陳怡彤
            ("0182792", "投資管理科", ""),  // 陳豊傑
            ("0183471", "營運管理科", ""),  // 林琮訓
            ("0183514", "投資管理科", ""),  // 王御丞
            ("0183899", "營運管理科", ""),  // 陳俊廷
            ("0007470", "應用服務科", ""),  // 林秀麗
            ("0013280", "應用服務科", ""),  // 郭禮睿
            ("0013714", "應用服務科", ""),  // 邱慶治
            ("0011969", "應用服務科", ""),  // 林依瑩
            ("0005721", "股務代理科", "科長"),  // 翁淑敏
            ("0012295", "股務代理科", ""),  // 番蕙珍
            ("0182802", "股務代理科", ""),  // 洪詩翔
            ("0014050", "開發四組", "組長"),  // 徐澎翊
            ("0009857", "應用服務科", ""),  // 蔡世中
            ("0180224", "應用服務科", "科長"),  // 賴彥愷
            ("0182123", "應用服務科", ""),  // 洪有謙
            ("0181908", "應用服務科", ""),  // 林曉欣
            ("0183112", "應用服務科", ""),  // 陳威凱
        ];

        private static void SeedSections(MigrationBuilder migrationBuilder)
        {
            foreach (var (name, team, kind, order) in Sections)
            {
                migrationBuilder.Sql($"""
                    INSERT INTO "Sections" ("Name", "TeamName", "Kind", "SortOrder", "Remark")
                    SELECT '{name}', '{team}', {(int)kind}, {order}, ''
                    WHERE NOT EXISTS (SELECT 1 FROM "Sections" WHERE "Name" = '{name}');
                    """);
            }
        }

        /// <summary>
        /// 以資產編號對應系統，不寫死 InfoSystems 的主鍵——各環境的主鍵不一定相同。
        /// 已軟刪除的系統不對應；環境裡不存在的資產編號就自然對不到，不會出錯。
        /// </summary>
        private static void SeedSectionSystems(MigrationBuilder migrationBuilder)
        {
            foreach (var (section, codes) in SectionSystemCodes)
            {
                var list = string.Join(", ", codes.Select(c => $"'{c}'"));
                migrationBuilder.Sql($"""
                    INSERT INTO "SectionSystems" ("SectionId", "InfoSystemId")
                    SELECT s."Id", i."Id"
                    FROM "Sections" s JOIN "InfoSystems" i ON i."SystemCode" IN ({list})
                    WHERE s."Name" = '{section}' AND i."IsDeleted" = 0
                      AND NOT EXISTS (SELECT 1 FROM "SectionSystems" x
                                      WHERE x."SectionId" = s."Id" AND x."InfoSystemId" = i."Id");
                    """);
            }
        }

        /// <summary>
        /// 依員編指定科別，組別跟著科別帶。人員檔裡沒有的人（例如蔡逸君）不指定科別——
        /// 授權只照業務端提供的名單給，舊資料裡的科別文字不沿用。
        ///
        /// 職稱（組長、科長）寫進備註，那一欄決定誰出現在「SW-應用系統主管」的下拉；
        /// 只補空白的，已經寫了其他內容的不覆蓋。
        /// </summary>
        private static void AssignEmployees(MigrationBuilder migrationBuilder)
        {
            foreach (var (empNo, section, note) in EmployeeSections)
            {
                migrationBuilder.Sql($"""
                    UPDATE "Employees"
                    SET "SectionId" = (SELECT "Id" FROM "Sections" WHERE "Name" = '{section}'),
                        "TeamName"  = (SELECT "TeamName" FROM "Sections" WHERE "Name" = '{section}')
                    WHERE "EmpNo" = '{empNo}';
                    """);

                if (note is "組長" or "科長")
                {
                    migrationBuilder.Sql($"""
                        UPDATE "Employees" SET "Remark" = '{note}'
                        WHERE "EmpNo" = '{empNo}' AND "Remark" = '';
                        """);
                }
            }
        }

        /// <summary>
        /// 依人員檔的「註」調整角色：管理員→管理者、組長／科長→主管。<b>只調高、不調低</b>——
        /// 各環境可能另外設過管理者，這裡不去動它們。
        /// </summary>
        private static void RaiseRoles(MigrationBuilder migrationBuilder)
        {
            foreach (var (empNo, _, note) in EmployeeSections)
            {
                var role = note switch
                {
                    "管理員" => (int)Models.UserRole.Admin,
                    "組長" or "科長" => (int)Models.UserRole.Manager,
                    _ => (int?)null,
                };
                if (role is null) continue;

                migrationBuilder.Sql($"""
                    UPDATE "AppUsers" SET "Role" = {role}
                    WHERE "EmpNo" = '{empNo}' AND "Role" < {role};
                    """);
            }
        }
    }
}

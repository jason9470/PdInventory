using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <summary>
    /// 黃鈺棠的重複帳號：0012478 併入 0014078（業務端 0918）。
    ///
    /// 0904 依舊名冊補建帳號時他的員編是 0012478，之後名冊（以及 0917 的人員檔）改成 0014078，
    /// 當時改人員表的員編不會連帶改帳號，於是 0014078 又補建了一個，同一個人兩個帳號。
    /// 0918 起改員編時帳號會跟著走（UserProvisioning.SyncUserForEmployeeAsync），不會再發生。
    ///
    /// 合併內容：角色取兩者較高者、最後登入時間取較晚者，軌跡欄位裡的「(0012478)」改成「(0014078)」，
    /// 最後停用 0012478。兩個環境查過都沒有軌跡引用、也都沒登入過，但仍照完整流程寫，以防甲方環境之後有異動。
    ///
    /// 每一句都限定「0014078 確實是人員表裡的黃鈺棠、而且兩個帳號都還有效」才動作，重跑不會有影響。
    /// </summary>
    public partial class MergeDuplicateAccount0012478 : Migration
    {
        private const string BothActive = """
            EXISTS (SELECT 1 FROM "Employees" e WHERE e."EmpNo" = '0014078' AND e."Name" = '黃鈺棠' AND e."IsDeleted" = 0)
            AND EXISTS (SELECT 1 FROM "AppUsers" n WHERE n."EmpNo" = '0014078' AND n."IsDeleted" = 0)
            AND EXISTS (SELECT 1 FROM "AppUsers" o WHERE o."EmpNo" = '0012478' AND o."IsDeleted" = 0)
            """;

        private const string DeletedMarker = "(0918 併入 0014078)";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 角色取較高者、最後登入取較晚者（舊帳號的值以子查詢帶入；BothActive 保證子查詢有值）
            migrationBuilder.Sql($"""
                UPDATE "AppUsers" SET
                    "Role" = MAX("Role", (SELECT o."Role" FROM "AppUsers" o WHERE o."EmpNo" = '0012478' AND o."IsDeleted" = 0)),
                    "LastLoginAt" = CASE
                        WHEN (SELECT o."LastLoginAt" FROM "AppUsers" o WHERE o."EmpNo" = '0012478' AND o."IsDeleted" = 0)
                             > COALESCE("LastLoginAt", '')
                        THEN (SELECT o."LastLoginAt" FROM "AppUsers" o WHERE o."EmpNo" = '0012478' AND o."IsDeleted" = 0)
                        ELSE "LastLoginAt" END
                WHERE "EmpNo" = '0014078' AND "IsDeleted" = 0 AND {BothActive};
                """);

            // 2. 軌跡欄位裡記成舊員編的，改記新員編。必須在停用之前做，否則 BothActive 不成立
            migrationBuilder.Sql($"""
                CREATE TEMP TABLE "_merge0012478" AS SELECT 1 AS "Go" WHERE {BothActive};
                """);
            migrationBuilder.Sql("""
                UPDATE "InfoSystems" SET "CreatedBy" = replace("CreatedBy", '(0012478)', '(0014078)')
                WHERE "CreatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "InfoSystems" SET "UpdatedBy" = replace("UpdatedBy", '(0012478)', '(0014078)')
                WHERE "UpdatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "InfoSystems" SET "DeletedBy" = replace("DeletedBy", '(0012478)', '(0014078)')
                WHERE "DeletedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "DataAssets" SET "CreatedBy" = replace("CreatedBy", '(0012478)', '(0014078)')
                WHERE "CreatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "DataAssets" SET "UpdatedBy" = replace("UpdatedBy", '(0012478)', '(0014078)')
                WHERE "UpdatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "DataAssets" SET "DeletedBy" = replace("DeletedBy", '(0012478)', '(0014078)')
                WHERE "DeletedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "SystemInventories" SET "CreatedBy" = replace("CreatedBy", '(0012478)', '(0014078)')
                WHERE "CreatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "SystemInventories" SET "UpdatedBy" = replace("UpdatedBy", '(0012478)', '(0014078)')
                WHERE "UpdatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "SystemInventories" SET "DeletedBy" = replace("DeletedBy", '(0012478)', '(0014078)')
                WHERE "DeletedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "InventoryItems" SET "CreatedBy" = replace("CreatedBy", '(0012478)', '(0014078)')
                WHERE "CreatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "InventoryItems" SET "UpdatedBy" = replace("UpdatedBy", '(0012478)', '(0014078)')
                WHERE "UpdatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "InventoryItems" SET "DeletedBy" = replace("DeletedBy", '(0012478)', '(0014078)')
                WHERE "DeletedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "TransferRecords" SET "CreatedBy" = replace("CreatedBy", '(0012478)', '(0014078)')
                WHERE "CreatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "TransferRecords" SET "UpdatedBy" = replace("UpdatedBy", '(0012478)', '(0014078)')
                WHERE "UpdatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "TransferRecords" SET "DeletedBy" = replace("DeletedBy", '(0012478)', '(0014078)')
                WHERE "DeletedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "InventoryNotes" SET "CreatedBy" = replace("CreatedBy", '(0012478)', '(0014078)')
                WHERE "CreatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "InventoryNotes" SET "UpdatedBy" = replace("UpdatedBy", '(0012478)', '(0014078)')
                WHERE "UpdatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "AppUsers" SET "CreatedBy" = replace("CreatedBy", '(0012478)', '(0014078)')
                WHERE "CreatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "AppUsers" SET "UpdatedBy" = replace("UpdatedBy", '(0012478)', '(0014078)')
                WHERE "UpdatedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "AppUsers" SET "DeletedBy" = replace("DeletedBy", '(0012478)', '(0014078)')
                WHERE "DeletedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""
                UPDATE "Employees" SET "DeletedBy" = replace("DeletedBy", '(0012478)', '(0014078)')
                WHERE "DeletedBy" LIKE '%(0012478)%' AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);

            // 3. 停用舊帳號（軟刪除，之後用 0012478 登入會看到「帳號已停用」）
            migrationBuilder.Sql($"""
                UPDATE "AppUsers" SET
                    "IsDeleted" = 1,
                    "DeletedAt" = strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'),
                    "DeletedBy" = '{DeletedMarker}'
                WHERE "EmpNo" = '0012478' AND "IsDeleted" = 0 AND EXISTS (SELECT 1 FROM "_merge0012478");
                """);
            migrationBuilder.Sql("""DROP TABLE "_merge0012478";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 只復原舊帳號；角色、登入時間與軌跡欄位的改寫不還原（分不出哪些是合併改的）
            migrationBuilder.Sql($"""
                UPDATE "AppUsers" SET "IsDeleted" = 0, "DeletedAt" = NULL, "DeletedBy" = ''
                WHERE "EmpNo" = '0012478' AND "DeletedBy" = '{DeletedMarker}';
                """);
        }
    }
}

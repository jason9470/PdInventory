using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <summary>
    /// 業務端 0918：來源試算表的「SW-修改者／SW-修改時間／DA-修改時間」併入軌跡欄位 UpdatedBy／UpdatedAt，
    /// 格式統一為「許稚苓(0005784)」「2026-09-11 14:06:07」。
    ///
    /// 0910 之後在系統裡存過檔的資料，兩套欄位是同一刻寫入的，軌跡欄位已經是正確格式，不動；
    /// 沒存過檔的，軌跡欄位只是匯入時的「系統匯入」，改用來源試算表的值。
    /// 每一句都有條件，重跑不會把已經合併好的值再蓋一次。
    /// </summary>
    public partial class MergeSourceSheetModifiedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 資料要在刪欄位之前搬：SQLite 刪欄位會重建資料表，EF 把重建延到最後才做 ──
            // 1. SW：軌跡欄位還不是真人（0910 以前沒在系統裡存過檔）的，改用來源試算表的修改者與時間。
            //    來源寫法「黃美倫 AngelaH (Yuanta)」「2026/8/28 08:16」→「黃美倫(0006341)」「2026-08-28 08:16:00」
            migrationBuilder.Sql("""
                UPDATE "InfoSystems" SET
                    "UpdatedBy" = CASE WHEN TRIM("SwModifiedBy") = '' THEN '' ELSE COALESCE((SELECT e."Name" || '(' || e."EmpNo" || ')' FROM "Employees" e WHERE e."IsDeleted" = 0 AND e."EmpNo" <> '' AND e."Name" = TRIM(CASE WHEN instr(TRIM("SwModifiedBy"), ' ') > 0 THEN substr(TRIM("SwModifiedBy"), 1, instr(TRIM("SwModifiedBy"), ' ') - 1) ELSE TRIM("SwModifiedBy") END)), TRIM(CASE WHEN instr(TRIM("SwModifiedBy"), ' ') > 0 THEN substr(TRIM("SwModifiedBy"), 1, instr(TRIM("SwModifiedBy"), ' ') - 1) ELSE TRIM("SwModifiedBy") END)) END,
                    "UpdatedAt" = printf('%04d-%02d-%02d %02d:%02d:00', CAST(substr(TRIM("SwModifiedTime"), 1, instr(TRIM("SwModifiedTime"), '/') - 1) AS INTEGER), CAST(substr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), 1, instr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), '/') - 1) AS INTEGER), CAST(substr(substr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), instr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), '/') + 1), 1, instr(substr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), instr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), '/') + 1), ' ') - 1) AS INTEGER), CAST(substr(substr(substr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), instr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), '/') + 1), instr(substr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), instr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), '/') + 1), ' ') + 1), 1, instr(substr(substr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), instr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), '/') + 1), instr(substr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), instr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), '/') + 1), ' ') + 1), ':') - 1) AS INTEGER), CAST(substr(substr(substr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), instr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), '/') + 1), instr(substr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), instr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), '/') + 1), ' ') + 1), instr(substr(substr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), instr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), '/') + 1), instr(substr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), instr(substr(TRIM("SwModifiedTime"), instr(TRIM("SwModifiedTime"), '/') + 1), '/') + 1), ' ') + 1), ':') + 1) AS INTEGER))
                WHERE "UpdatedBy" NOT GLOB '*([0-9][0-9][0-9][0-9][0-9][0-9][0-9])' AND TRIM("SwModifiedTime") <> '';
                """);

            // 2. DA：來源只有修改時間，沒有修改者 → 時間照搬、修改者留空
            migrationBuilder.Sql("""
                UPDATE "DataAssets" SET
                    "UpdatedBy" = '',
                    "UpdatedAt" = printf('%04d-%02d-%02d %02d:%02d:00', CAST(substr(TRIM("DaModifiedTime"), 1, instr(TRIM("DaModifiedTime"), '/') - 1) AS INTEGER), CAST(substr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), 1, instr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), '/') - 1) AS INTEGER), CAST(substr(substr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), instr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), '/') + 1), 1, instr(substr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), instr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), '/') + 1), ' ') - 1) AS INTEGER), CAST(substr(substr(substr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), instr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), '/') + 1), instr(substr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), instr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), '/') + 1), ' ') + 1), 1, instr(substr(substr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), instr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), '/') + 1), instr(substr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), instr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), '/') + 1), ' ') + 1), ':') - 1) AS INTEGER), CAST(substr(substr(substr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), instr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), '/') + 1), instr(substr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), instr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), '/') + 1), ' ') + 1), instr(substr(substr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), instr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), '/') + 1), instr(substr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), instr(substr(TRIM("DaModifiedTime"), instr(TRIM("DaModifiedTime"), '/') + 1), '/') + 1), ' ') + 1), ':') + 1) AS INTEGER))
                WHERE "UpdatedBy" NOT GLOB '*([0-9][0-9][0-9][0-9][0-9][0-9][0-9])' AND TRIM("DaModifiedTime") <> '';
                """);

            // 3. 時間精確到秒（業務端 0918 指定「2026-09-11 14:06:07」），舊資料的小數秒去掉
            migrationBuilder.Sql("""
                UPDATE "InfoSystems" SET "CreatedAt" = substr("CreatedAt", 1, 19) WHERE length("CreatedAt") > 19;
                UPDATE "InfoSystems" SET "UpdatedAt" = substr("UpdatedAt", 1, 19) WHERE length("UpdatedAt") > 19;
                UPDATE "InfoSystems" SET "DeletedAt" = substr("DeletedAt", 1, 19) WHERE length("DeletedAt") > 19;
                UPDATE "DataAssets" SET "CreatedAt" = substr("CreatedAt", 1, 19) WHERE length("CreatedAt") > 19;
                UPDATE "DataAssets" SET "UpdatedAt" = substr("UpdatedAt", 1, 19) WHERE length("UpdatedAt") > 19;
                UPDATE "DataAssets" SET "DeletedAt" = substr("DeletedAt", 1, 19) WHERE length("DeletedAt") > 19;
                UPDATE "SystemInventories" SET "CreatedAt" = substr("CreatedAt", 1, 19) WHERE length("CreatedAt") > 19;
                UPDATE "SystemInventories" SET "UpdatedAt" = substr("UpdatedAt", 1, 19) WHERE length("UpdatedAt") > 19;
                UPDATE "SystemInventories" SET "DeletedAt" = substr("DeletedAt", 1, 19) WHERE length("DeletedAt") > 19;
                UPDATE "InventoryItems" SET "CreatedAt" = substr("CreatedAt", 1, 19) WHERE length("CreatedAt") > 19;
                UPDATE "InventoryItems" SET "UpdatedAt" = substr("UpdatedAt", 1, 19) WHERE length("UpdatedAt") > 19;
                UPDATE "InventoryItems" SET "DeletedAt" = substr("DeletedAt", 1, 19) WHERE length("DeletedAt") > 19;
                UPDATE "TransferRecords" SET "CreatedAt" = substr("CreatedAt", 1, 19) WHERE length("CreatedAt") > 19;
                UPDATE "TransferRecords" SET "UpdatedAt" = substr("UpdatedAt", 1, 19) WHERE length("UpdatedAt") > 19;
                UPDATE "TransferRecords" SET "DeletedAt" = substr("DeletedAt", 1, 19) WHERE length("DeletedAt") > 19;
                """);

            migrationBuilder.DropColumn(
                name: "SwModifiedBy",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "SwModifiedTime",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaModifiedTime",
                table: "DataAssets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SwModifiedBy",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SwModifiedTime",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaModifiedTime",
                table: "DataAssets",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // 盡量還原：修改者照搬，時間轉回來源試算表的寫法「2026/08/28 08:16」
            migrationBuilder.Sql("""
                UPDATE "InfoSystems" SET "SwModifiedBy" = "UpdatedBy",
                    "SwModifiedTime" = COALESCE(strftime('%Y/%m/%d %H:%M', "UpdatedAt"), '');
                """);
            migrationBuilder.Sql("""
                UPDATE "DataAssets" SET "DaModifiedTime" = COALESCE(strftime('%Y/%m/%d %H:%M', "UpdatedAt"), '');
                """);
        }
    }
}

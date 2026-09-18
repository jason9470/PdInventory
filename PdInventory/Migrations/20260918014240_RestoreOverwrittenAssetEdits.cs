using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <summary>
    /// 補回 0917 部署時被覆蓋的 6 筆資產異動（0918）。
    ///
    /// 發生了什麼：發佈設定當時會把 App_Data（資料庫本體）一起帶出去，上線環境的 pdinventory.db
    /// 因此被 repo 裡的開發用資料庫整個蓋掉，甲方 9/15～9/17 早上改的內容跟著消失。
    /// 發佈設定已改成排除 App_Data（FolderProfile.pubxml），不會再發生。
    ///
    /// 這裡只補「被蓋掉、而且之後沒有人再動過」的欄位：每一句都要求那些欄位現在仍等於被蓋掉的值，
    /// 甲方在 9/17 下午之後自己改過的一律不碰。修改者與修改時間一併還原成當初真正改的人。
    ///
    /// 值取自甲方 0917 提供的資料庫，由 scratchpad 的產生器直接轉出，沒有手抄。
    /// 在已經補過、或本來就沒被蓋到的環境（例如開發機）重跑不會有任何影響。
    /// </summary>
    public partial class RestoreOverwrittenAssetEdits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SW-021：SwAppMaintainerDeputy（楊常鑫(0020906) 2026-09-16 16:46:59）
            migrationBuilder.Sql("""
                UPDATE "InfoSystems" SET
                        "SwAppMaintainerDeputy" = '林子耕',
                        "UpdatedBy" = CASE WHEN COALESCE("UpdatedAt", '') < '2026-09-16 16:46:59' THEN '楊常鑫(0020906)' ELSE "UpdatedBy" END,
                        "UpdatedAt" = CASE WHEN COALESCE("UpdatedAt", '') < '2026-09-16 16:46:59' THEN '2026-09-16 16:46:59' ELSE "UpdatedAt" END
                WHERE "SystemCode" = 'SW-021'
                  AND "SwAppMaintainerDeputy" = '郭禮睿';
                """);

            // SW-033：SwDeveloper（蔡嘉駒(0050082) 2026-09-17 07:44:22）
            migrationBuilder.Sql("""
                UPDATE "InfoSystems" SET
                        "SwDeveloper" = '吳怡萱/陳佳淇/陳冠州/陳宥鈊/彭先達/趙妤瑄/蔡文斌',
                        "UpdatedBy" = CASE WHEN COALESCE("UpdatedAt", '') < '2026-09-17 07:44:22' THEN '蔡嘉駒(0050082)' ELSE "UpdatedBy" END,
                        "UpdatedAt" = CASE WHEN COALESCE("UpdatedAt", '') < '2026-09-17 07:44:22' THEN '2026-09-17 07:44:22' ELSE "UpdatedAt" END
                WHERE "SystemCode" = 'SW-033'
                  AND "SwDeveloper" = '蔡文斌/陳冠州/陳佳淇/彭先達/吳怡萱/趙妤瑄';
                """);

            // SW-036：SwAppMaintainerDeputy（楊常鑫(0020906) 2026-09-16 16:46:13）
            migrationBuilder.Sql("""
                UPDATE "InfoSystems" SET
                        "SwAppMaintainerDeputy" = '林子耕',
                        "UpdatedBy" = CASE WHEN COALESCE("UpdatedAt", '') < '2026-09-16 16:46:13' THEN '楊常鑫(0020906)' ELSE "UpdatedBy" END,
                        "UpdatedAt" = CASE WHEN COALESCE("UpdatedAt", '') < '2026-09-16 16:46:13' THEN '2026-09-16 16:46:13' ELSE "UpdatedAt" END
                WHERE "SystemCode" = 'SW-036'
                  AND "SwAppMaintainerDeputy" = '邱慶治';
                """);

            // SW-041：SwAppMaintainerDeputy（楊常鑫(0020906) 2026-09-16 16:47:33）
            migrationBuilder.Sql("""
                UPDATE "InfoSystems" SET
                        "SwAppMaintainerDeputy" = '林子耕',
                        "UpdatedBy" = CASE WHEN COALESCE("UpdatedAt", '') < '2026-09-16 16:47:33' THEN '楊常鑫(0020906)' ELSE "UpdatedBy" END,
                        "UpdatedAt" = CASE WHEN COALESCE("UpdatedAt", '') < '2026-09-16 16:47:33' THEN '2026-09-16 16:47:33' ELSE "UpdatedAt" END
                WHERE "SystemCode" = 'SW-041'
                  AND "SwAppMaintainerDeputy" = '郭禮睿';
                """);

            // SW-046：SwAppMaintainer、SwAppMaintainerDeputy、SwReviewer（楊常鑫(0020906) 2026-09-16 16:55:53）
            migrationBuilder.Sql("""
                UPDATE "InfoSystems" SET
                        "SwAppMaintainer" = '林子耕',
                        "SwAppMaintainerDeputy" = '楊常鑫',
                        "SwReviewer" = '楊常鑫',
                        "UpdatedBy" = CASE WHEN COALESCE("UpdatedAt", '') < '2026-09-16 16:55:53' THEN '楊常鑫(0020906)' ELSE "UpdatedBy" END,
                        "UpdatedAt" = CASE WHEN COALESCE("UpdatedAt", '') < '2026-09-16 16:55:53' THEN '2026-09-16 16:55:53' ELSE "UpdatedAt" END
                WHERE "SystemCode" = 'SW-046'
                  AND "SwAppMaintainer" = '邱慶治'
                      AND "SwAppMaintainerDeputy" = '郭禮睿'
                      AND "SwReviewer" = '邱慶治';
                """);

            // SW-003：SwRelatedSystems（邱慶霖(0005837) 2026-09-16 15:03:04）
            migrationBuilder.Sql("""
                UPDATE "InfoSystems" SET
                        "SwRelatedSystems" = 'SW-004(電子交易API);SW-020(電子交易系統);SW-030(經紀前台系統);SW-062(行情報價系統);SW-064(主動回報系統)',
                        "UpdatedBy" = CASE WHEN COALESCE("UpdatedAt", '') < '2026-09-16 15:03:04' THEN '邱慶霖(0005837)' ELSE "UpdatedBy" END,
                        "UpdatedAt" = CASE WHEN COALESCE("UpdatedAt", '') < '2026-09-16 15:03:04' THEN '2026-09-16 15:03:04' ELSE "UpdatedAt" END
                WHERE "SystemCode" = 'SW-003'
                  AND "SwRelatedSystems" = 'SW-004(電子交易API);SW-020(電子交易統);SW-030(經紀前台系統);SW-062(行情報價系統);SW-064(主動回報系統)';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 不還原：這裡補的就是正確內容，倒回去等於再把甲方的異動蓋掉一次
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <summary>
    /// 0914～0916 期間直接改在資料庫裡、沒有跟著 migration 走的兩件資料異動，
    /// 補成 migration 讓已上線的環境部署時自動套用。
    ///
    /// 一、DA-確認-資料備份方式的 8 個選項（0916 選項化）。沒有這些選項，
    ///     新增與編輯畫面的勾選清單會是空的。
    /// 二、部門／人員／資管維運三張維護表的備註拿掉「甲方」（0914）。
    ///
    /// 每一句都寫成有條件的，理由是對方環境的資料在部署前已經被使用者動過：
    ///   - 選項用 WHERE NOT EXISTS，已經有的不會重複建，也不寫死主鍵（對方可能自己加過選項）
    ///   - 備註只改「還是舊字串」的列，對方改寫過的備註不會被蓋掉
    /// 因此在已經做過這兩件事的資料庫（例如開發環境）上跑，什麼都不會發生。
    /// </summary>
    public partial class SeedDaBackupConfirmOptionsAndCleanRemarks : Migration
    {
        private static readonly string[] BackupConfirmOptions =
        [
            "本機檔案備份",
            "本地存放",
            "資料庫同地備份",
            "資料庫同地抄寫",
            "資料庫異地抄寫",
            "資料庫同地備份成檔案",
            "異地鏡像抄寫",
            "無",
        ];

        private const string OldRemarkFragment = "不在甲方 0903 ";
        private const string NewRemarkFragment = "不在 0903 匯入的";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            for (var i = 0; i < BackupConfirmOptions.Length; i++)
            {
                var value = BackupConfirmOptions[i];
                migrationBuilder.Sql($"""
                    INSERT INTO "FieldOptionItems" ("FieldName", "Value", "SortOrder", "Remark")
                    SELECT 'DaBackupConfirm', '{value}', {i + 1}, ''
                    WHERE NOT EXISTS (
                        SELECT 1 FROM "FieldOptionItems"
                        WHERE "FieldName" = 'DaBackupConfirm' AND "Value" = '{value}');
                    """);
            }

            foreach (var table in new[] { "Departments", "Employees", "OpsStaffs" })
            {
                migrationBuilder.Sql($"""
                    UPDATE "{table}"
                    SET "Remark" = REPLACE("Remark", '{OldRemarkFragment}', '{NewRemarkFragment}')
                    WHERE "Remark" LIKE '%{OldRemarkFragment}%';
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "Departments", "Employees", "OpsStaffs" })
            {
                migrationBuilder.Sql($"""
                    UPDATE "{table}"
                    SET "Remark" = REPLACE("Remark", '{NewRemarkFragment}', '{OldRemarkFragment}')
                    WHERE "Remark" LIKE '%{NewRemarkFragment}%';
                    """);
            }

            // 還原選項時只刪沒人在用的：已經被資料勾選的選項刪掉，那些資料就變成孤兒值
            foreach (var value in BackupConfirmOptions)
            {
                migrationBuilder.Sql($"""
                    DELETE FROM "FieldOptionItems"
                    WHERE "FieldName" = 'DaBackupConfirm' AND "Value" = '{value}'
                      AND NOT EXISTS (
                          SELECT 1 FROM "DataAssets"
                          WHERE '/' || "DaBackupConfirm" || '/' LIKE '%/{value}/%');
                    """);
            }
        }
    }
}

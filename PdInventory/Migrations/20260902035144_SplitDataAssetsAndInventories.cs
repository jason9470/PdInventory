using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <inheritdoc />
    public partial class SplitDataAssetsAndInventories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SystemCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DaAssetCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DaAssetType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DaDescription = table.Column<string>(type: "TEXT", nullable: false),
                    DaBackupMethod = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    DaRetentionPeriod = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DaHasSensitiveData = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DaUserUnit = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DaRemark = table.Column<string>(type: "TEXT", nullable: false),
                    DaBackupConfirm = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    DaReviewer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DaModifiedTime = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemInventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SystemCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DbName = table.Column<string>(type: "TEXT", nullable: false),
                    BackupLocation = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    BackupCycle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ExternalUnitName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    HasLog = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AccessCreate = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AccessDelete = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AccessCopy = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FileDescription = table.Column<string>(type: "TEXT", nullable: false),
                    SpecialData = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SubjectCount = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RetentionPeriod = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Remark = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemInventories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemInventories_SystemCode",
                table: "SystemInventories",
                column: "SystemCode",
                unique: true,
                filter: "\"SystemCode\" <> ''");

            // 先建表、搬資料，最後才刪欄位——順序反了資料就沒了。
            // 只搬有內容的列：原本三組欄位擠在同一列，很多列的 DA 或盤點表其實是空的。
            migrationBuilder.Sql(@"
                INSERT INTO DataAssets (SystemCode, DaAssetCode, DaAssetType, DaDescription, DaBackupMethod, DaRetentionPeriod, DaHasSensitiveData, DaUserUnit, DaRemark, DaBackupConfirm, DaReviewer, DaModifiedTime, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, RowVersion)
                SELECT SystemCode, DaAssetCode, DaAssetType, DaDescription, DaBackupMethod, DaRetentionPeriod, DaHasSensitiveData, DaUserUnit, DaRemark, DaBackupConfirm, DaReviewer, DaModifiedTime, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, RowVersion
                FROM InfoSystems
                WHERE IsDeleted = 0 AND (TRIM(IFNULL(DaAssetCode, '')) <> '' OR TRIM(IFNULL(DaAssetType, '')) <> '' OR TRIM(IFNULL(DaDescription, '')) <> '' OR TRIM(IFNULL(DaBackupMethod, '')) <> '' OR TRIM(IFNULL(DaRetentionPeriod, '')) <> '' OR TRIM(IFNULL(DaHasSensitiveData, '')) <> '' OR TRIM(IFNULL(DaUserUnit, '')) <> '' OR TRIM(IFNULL(DaRemark, '')) <> '' OR TRIM(IFNULL(DaBackupConfirm, '')) <> '' OR TRIM(IFNULL(DaReviewer, '')) <> '' OR TRIM(IFNULL(DaModifiedTime, '')) <> '');");

            // 盤點表一定依附於某個軟體資產，沒有資產編號的不搬
            migrationBuilder.Sql(@"
                INSERT INTO SystemInventories (SystemCode, DbName, BackupLocation, BackupCycle, ExternalUnitName, HasLog, AccessCreate, AccessDelete, AccessCopy, FileDescription, SpecialData, SubjectCount, RetentionPeriod, Remark, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, RowVersion)
                SELECT SystemCode, DbName, BackupLocation, BackupCycle, ExternalUnitName, HasLog, AccessCreate, AccessDelete, AccessCopy, FileDescription, SpecialData, SubjectCount, RetentionPeriod, Remark, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, RowVersion
                FROM InfoSystems
                WHERE IsDeleted = 0 AND TRIM(IFNULL(SystemCode, '')) <> '' AND (TRIM(IFNULL(DbName, '')) <> '' OR TRIM(IFNULL(BackupLocation, '')) <> '' OR TRIM(IFNULL(BackupCycle, '')) <> '' OR TRIM(IFNULL(ExternalUnitName, '')) <> '' OR TRIM(IFNULL(HasLog, '')) <> '' OR TRIM(IFNULL(AccessCreate, '')) <> '' OR TRIM(IFNULL(AccessDelete, '')) <> '' OR TRIM(IFNULL(AccessCopy, '')) <> '' OR TRIM(IFNULL(FileDescription, '')) <> '' OR TRIM(IFNULL(SpecialData, '')) <> '' OR TRIM(IFNULL(SubjectCount, '')) <> '' OR TRIM(IFNULL(RetentionPeriod, '')) <> '' OR TRIM(IFNULL(Remark, '')) <> '');");

            migrationBuilder.DropColumn(
                name: "AccessCopy",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "AccessCreate",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "AccessDelete",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "BackupCycle",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "BackupLocation",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaAssetCode",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaAssetType",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaBackupConfirm",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaBackupMethod",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaDescription",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaHasSensitiveData",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaModifiedTime",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaRemark",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaRetentionPeriod",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaReviewer",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaUserUnit",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DbName",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "ExternalUnitName",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "FileDescription",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "HasLog",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "RetentionPeriod",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "SpecialData",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "SubjectCount",
                table: "InfoSystems");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataAssets");

            migrationBuilder.DropTable(
                name: "SystemInventories");

            migrationBuilder.AddColumn<string>(
                name: "AccessCopy",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccessCreate",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccessDelete",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BackupCycle",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BackupLocation",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaAssetCode",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaAssetType",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaBackupConfirm",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaBackupMethod",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaDescription",
                table: "InfoSystems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaHasSensitiveData",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaModifiedTime",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaRemark",
                table: "InfoSystems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaRetentionPeriod",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaReviewer",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaUserUnit",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DbName",
                table: "InfoSystems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalUnitName",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileDescription",
                table: "InfoSystems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HasLog",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "InfoSystems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RetentionPeriod",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpecialData",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubjectCount",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}

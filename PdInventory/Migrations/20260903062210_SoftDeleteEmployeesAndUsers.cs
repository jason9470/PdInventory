using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteEmployeesAndUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_Name",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_EmpNo",
                table: "AppUsers");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Employees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Employees",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Employees",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AppUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "AppUsers",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AppUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Name",
                table: "Employees",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_EmpNo",
                table: "AppUsers",
                column: "EmpNo",
                unique: true,
                filter: "\"IsDeleted\" = 0");

            // 五個模擬登入用的測試帳號（1111111~5555555）隨這次改版移除：
            // 帳號改為與人員表對接之後，能登入的人一律要在人員表裡有一筆，
            // 這些在公司員工目錄與人員表都不存在的假帳號沒有立足之地了。
            // 硬刪除而不是加停用註記——它們從來就不是真人，留著只會佔著權限畫面。
            migrationBuilder.Sql(@"
                DELETE FROM AssetOwners
                 WHERE AppUserId IN (SELECT Id FROM AppUsers
                                      WHERE EmpNo IN ('1111111','2222222','3333333','4444444','5555555'));");
            migrationBuilder.Sql(@"
                DELETE FROM AppUsers
                 WHERE EmpNo IN ('1111111','2222222','3333333','4444444','5555555');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_Name",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_EmpNo",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AppUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Name",
                table: "Employees",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_EmpNo",
                table: "AppUsers",
                column: "EmpNo",
                unique: true);
        }
    }
}

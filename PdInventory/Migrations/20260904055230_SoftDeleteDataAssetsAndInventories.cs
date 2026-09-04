using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteDataAssetsAndInventories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SystemInventories_SystemCode",
                table: "SystemInventories");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SystemInventories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SystemInventories",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SystemInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DataAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "DataAssets",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DataAssets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SystemInventories_SystemCode",
                table: "SystemInventories",
                column: "SystemCode",
                unique: true,
                filter: "\"SystemCode\" <> '' AND \"IsDeleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SystemInventories_SystemCode",
                table: "SystemInventories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SystemInventories");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SystemInventories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SystemInventories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DataAssets");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DataAssets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DataAssets");

            migrationBuilder.CreateIndex(
                name: "IX_SystemInventories_SystemCode",
                table: "SystemInventories",
                column: "SystemCode",
                unique: true,
                filter: "\"SystemCode\" <> ''");
        }
    }
}

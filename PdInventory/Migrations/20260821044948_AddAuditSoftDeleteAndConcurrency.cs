using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditSoftDeleteAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InfoSystems_SystemCode",
                table: "InfoSystems");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TransferRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "TransferRecords",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RowVersion",
                table: "TransferRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TransferRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TransferRecords",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "InventoryItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "InventoryItems",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RowVersion",
                table: "InventoryItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "InventoryItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "InventoryItems",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "InfoSystems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "InfoSystems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InfoSystems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RowVersion",
                table: "InfoSystems",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "InfoSystems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_InfoSystems_SystemCode",
                table: "InfoSystems",
                column: "SystemCode",
                unique: true,
                filter: "\"SystemCode\" <> '' AND \"IsDeleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InfoSystems_SystemCode",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TransferRecords");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TransferRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TransferRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TransferRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TransferRecords");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "InfoSystems");

            migrationBuilder.CreateIndex(
                name: "IX_InfoSystems_SystemCode",
                table: "InfoSystems",
                column: "SystemCode",
                unique: true,
                filter: "\"SystemCode\" <> ''");
        }
    }
}

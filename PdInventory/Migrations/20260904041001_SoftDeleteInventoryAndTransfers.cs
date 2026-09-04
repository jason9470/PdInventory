using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteInventoryAndTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransferRecords_SeqNo",
                table: "TransferRecords");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_SeqNo",
                table: "InventoryItems");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TransferRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TransferRecords",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TransferRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "InventoryItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "InventoryItems",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InventoryItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecords_SeqNo",
                table: "TransferRecords",
                column: "SeqNo",
                unique: true,
                filter: "\"SeqNo\" <> '' AND \"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SeqNo",
                table: "InventoryItems",
                column: "SeqNo",
                unique: true,
                filter: "\"SeqNo\" <> '' AND \"IsDeleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransferRecords_SeqNo",
                table: "TransferRecords");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_SeqNo",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TransferRecords");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TransferRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TransferRecords");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InventoryItems");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecords_SeqNo",
                table: "TransferRecords",
                column: "SeqNo",
                unique: true,
                filter: "\"SeqNo\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SeqNo",
                table: "InventoryItems",
                column: "SeqNo",
                unique: true,
                filter: "\"SeqNo\" <> ''");
        }
    }
}

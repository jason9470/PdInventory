using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueKeyIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_InfoSystems_SystemCode",
                table: "InfoSystems",
                column: "SystemCode",
                unique: true,
                filter: "\"SystemCode\" <> ''");
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

            migrationBuilder.DropIndex(
                name: "IX_InfoSystems_SystemCode",
                table: "InfoSystems");
        }
    }
}

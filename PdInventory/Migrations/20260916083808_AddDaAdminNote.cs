using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <inheritdoc />
    public partial class AddDaAdminNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DaAdminNote",
                table: "DataAssets",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaAdminNote",
                table: "DataAssets");
        }
    }
}

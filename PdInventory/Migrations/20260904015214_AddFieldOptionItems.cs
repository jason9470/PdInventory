using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldOptionItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FieldOptionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FieldName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Remark = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldOptionItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldOptionItems_FieldName_Value",
                table: "FieldOptionItems",
                columns: new[] { "FieldName", "Value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FieldOptionItems");
        }
    }
}

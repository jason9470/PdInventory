using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <summary>
    /// 權限改以科別為中心（0917）的<b>結構</b>部分：人員 → 科別 → 系統，管理者不受限制。
    ///
    /// 新增科別與「科負責的系統」兩張表、人員表的科別從自由文字改成指向科別表；
    /// 拿掉逐筆資產授權 AssetOwners（全系統只有 2 筆，都落在當事人自己的科別裡）。
    /// 資料匯入在下一個 migration（ImportSectionPermissionsData）。
    ///
    /// 結構與資料分成兩個 migration，是因為 SQLite 刪欄位與加外鍵都要重建資料表，
    /// 而 EF 會把重建延到 migration 結尾才做；資料 SQL 若放在同一個 migration，
    /// 會在重建之前執行，結果不保證。
    ///
    /// Down 只還原結構，資料不還原：舊的科別文字與那 2 筆資產授權救不回來。
    /// </summary>
    public partial class SectionBasedPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetOwners");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "Employees");

            migrationBuilder.AddColumn<int>(
                name: "SectionId",
                table: "Employees",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TeamName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Remark = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SectionSystems",
                columns: table => new
                {
                    SectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    InfoSystemId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionSystems", x => new { x.SectionId, x.InfoSystemId });
                    table.ForeignKey(
                        name: "FK_SectionSystems_InfoSystems_InfoSystemId",
                        column: x => x.InfoSystemId,
                        principalTable: "InfoSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SectionSystems_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SectionId",
                table: "Employees",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_Name",
                table: "Sections",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionSystems_InfoSystemId",
                table: "SectionSystems",
                column: "InfoSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Sections_SectionId",
                table: "Employees",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Sections_SectionId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "SectionSystems");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SectionId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "Employees");

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "Employees",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AssetOwners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    InfoSystemId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetOwners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetOwners_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetOwners_InfoSystems_InfoSystemId",
                        column: x => x.InfoSystemId,
                        principalTable: "InfoSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetOwners_AppUserId_InfoSystemId",
                table: "AssetOwners",
                columns: new[] { "AppUserId", "InfoSystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetOwners_InfoSystemId",
                table: "AssetOwners",
                column: "InfoSystemId");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <inheritdoc />
    public partial class MergeSharedSwDaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaAssetValue",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaAvailability",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaConfidentiality",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaCustodianUnit",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaIntegrity",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaLocation",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaOwnerUnit",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaRiskOwner",
                table: "InfoSystems");

            migrationBuilder.DropColumn(
                name: "DaStatus",
                table: "InfoSystems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DaAssetValue",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaAvailability",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaConfidentiality",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaCustodianUnit",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaIntegrity",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaLocation",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaOwnerUnit",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaRiskOwner",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DaStatus",
                table: "InfoSystems",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdInventory.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DisplayCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    GroupName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Example = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ListKey = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PropertyName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Included = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportColumns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InfoSystems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeqNo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SystemCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SystemName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
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
                    SwStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwAssetType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwSystemCategory = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SwAdIntegration = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwOsVersion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwDbToolVersion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwThirdPartyComponents = table.Column<string>(type: "TEXT", nullable: false),
                    SwUserAccountGrant = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwProvidesAccountReport = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwRiskOwner = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwLocation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwOwnerUnit = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwCustodianUnit = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwUserUnit = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwConfidentiality = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SwIntegrity = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SwAvailability = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SwAssetValue = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SwBusinessContact = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwAppManager = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwAppMaintainer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwAppMaintainerDeputy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwOperator = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwDevMode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SwMaintMode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SwVendor = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwLanguage = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwVersionControl = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwApRepoPath = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SwDeployMethod = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwOpRepoPath = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SwCodeAccess = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwDeveloper = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwDeployer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwRpo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwLocalBackup = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwLocalBackupType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwLocalBackupFreq = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwRemoteBackup = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SwRemoteBackupType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwRemoteBackupFreq = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwLocalHa = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwLocalHaArch = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwRemoteHa = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwRemoteHaArch = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SwRto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwHasRecoveryPlan = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwHasDrDrill = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwRelatedSystems = table.Column<string>(type: "TEXT", nullable: false),
                    SwRemark = table.Column<string>(type: "TEXT", nullable: false),
                    SwHandlesPersonalData = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwHasUiAuth = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwKeepsPdTrail = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwProvidesApi = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwTrailLocation = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SwTrailStorage = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwBusinessOwnerUnit = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwIsCoreSystem = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SwReviewer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwModifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SwModifiedTime = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DaAssetCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DaAssetType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DaStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DaDescription = table.Column<string>(type: "TEXT", nullable: false),
                    DaBackupMethod = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    DaRetentionPeriod = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DaHasSensitiveData = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DaRiskOwner = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DaLocation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DaOwnerUnit = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DaCustodianUnit = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DaUserUnit = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DaConfidentiality = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DaIntegrity = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DaAvailability = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DaAssetValue = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DaRemark = table.Column<string>(type: "TEXT", nullable: false),
                    DaBackupConfirm = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    DaReviewer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DaModifiedTime = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoSystems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeqNo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DocumentName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SubjectCount = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SubjectType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HasSpecialData = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpecialDataLegalBasis = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    MinFieldCompliant = table.Column<bool>(type: "INTEGER", nullable: false),
                    SystemCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SystemName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SourceCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CompanyRole = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CollectProcedure = table.Column<string>(type: "TEXT", nullable: false),
                    CollectStatement = table.Column<string>(type: "TEXT", nullable: false),
                    CollectConsent = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessProcedure = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessDept = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProcessStatement = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessConsent = table.Column<string>(type: "TEXT", nullable: false),
                    TransferTarget = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    TransferContract = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    TransferMethod = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TransferCountry = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RetentionPaper = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RetentionDigital = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LocationPaper = table.Column<string>(type: "TEXT", nullable: false),
                    LocationDigital = table.Column<string>(type: "TEXT", nullable: false),
                    Disposal = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Remark = table.Column<string>(type: "TEXT", nullable: false),
                    RiskDataSeqNo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    RiskCategoryCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RiskCategoryName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RiskEvent = table.Column<string>(type: "TEXT", nullable: false),
                    RiskImpactLevel = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    RiskLikelihoodLevel = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    RiskRelatedRegulation = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    RiskControlDescription = table.Column<string>(type: "TEXT", nullable: false),
                    RiskEffectivenessLevel = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    RiskValue = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RiskImprovementPlan = table.Column<string>(type: "TEXT", nullable: false),
                    RiskUnitConfirm = table.Column<string>(type: "TEXT", nullable: false),
                    RiskRemark = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Purposes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purposes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CategoryName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EventDescription = table.Column<string>(type: "TEXT", nullable: false),
                    ControlReference = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskEffectivenessLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Effectiveness = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CheckResult = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskEffectivenessLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskImpactLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FinancialImpact = table.Column<string>(type: "TEXT", nullable: false),
                    ReputationImpact = table.Column<string>(type: "TEXT", nullable: false),
                    PrivacyImpact = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskImpactLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskLikelihoodLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Situation = table.Column<string>(type: "TEXT", nullable: false),
                    Frequency = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskLikelihoodLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransferRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeqNo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TransferType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SystemCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SystemName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PathName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ExternalUnit = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    InternalUnit = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ContentDescription = table.Column<string>(type: "TEXT", nullable: false),
                    SpecialData = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SubjectCount = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    InternationalTransfer = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Contract = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Remark = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItemCategories",
                columns: table => new
                {
                    CategoriesId = table.Column<int>(type: "INTEGER", nullable: false),
                    InventoryItemsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItemCategories", x => new { x.CategoriesId, x.InventoryItemsId });
                    table.ForeignKey(
                        name: "FK_InventoryItemCategories_Categories_CategoriesId",
                        column: x => x.CategoriesId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryItemCategories_InventoryItems_InventoryItemsId",
                        column: x => x.InventoryItemsId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItemPurposes",
                columns: table => new
                {
                    InventoryItemsId = table.Column<int>(type: "INTEGER", nullable: false),
                    PurposesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItemPurposes", x => new { x.InventoryItemsId, x.PurposesId });
                    table.ForeignKey(
                        name: "FK_InventoryItemPurposes_InventoryItems_InventoryItemsId",
                        column: x => x.InventoryItemsId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryItemPurposes_Purposes_PurposesId",
                        column: x => x.PurposesId,
                        principalTable: "Purposes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Code",
                table: "Categories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExportColumns_ListKey_PropertyName",
                table: "ExportColumns",
                columns: new[] { "ListKey", "PropertyName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemCategories_InventoryItemsId",
                table: "InventoryItemCategories",
                column: "InventoryItemsId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemPurposes_PurposesId",
                table: "InventoryItemPurposes",
                column: "PurposesId");

            migrationBuilder.CreateIndex(
                name: "IX_Purposes_Code",
                table: "Purposes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskCategories_Code",
                table: "RiskCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskEffectivenessLevels_Level",
                table: "RiskEffectivenessLevels",
                column: "Level",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskImpactLevels_Level",
                table: "RiskImpactLevels",
                column: "Level",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskLikelihoodLevels_Level",
                table: "RiskLikelihoodLevels",
                column: "Level",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExportColumns");

            migrationBuilder.DropTable(
                name: "InfoSystems");

            migrationBuilder.DropTable(
                name: "InventoryItemCategories");

            migrationBuilder.DropTable(
                name: "InventoryItemPurposes");

            migrationBuilder.DropTable(
                name: "RiskCategories");

            migrationBuilder.DropTable(
                name: "RiskEffectivenessLevels");

            migrationBuilder.DropTable(
                name: "RiskImpactLevels");

            migrationBuilder.DropTable(
                name: "RiskLikelihoodLevels");

            migrationBuilder.DropTable(
                name: "TransferRecords");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "Purposes");
        }
    }
}

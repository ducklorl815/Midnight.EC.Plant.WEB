using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Midnight.EC.Plant.WEB.Models.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlantSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantSpecies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScientificName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CommonName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ChineseName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Genus = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Family = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TaxonId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SourceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantSpecies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlantKnowledge",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpeciesId = table.Column<int>(type: "int", nullable: false),
                    LightRequirement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WaterRequirement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HumidityRequirement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemperatureMin = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TemperatureMax = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    SoilRequirement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FertilizerRequirement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dormancy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrowthSeason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepottingAdvice = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommonProblems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PestProblems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiseaseProblems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CareSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataVersion = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantKnowledge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantKnowledge_PlantSpecies_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "PlantSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantKnowledgeSyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpeciesId = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    RequestUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResponseHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantKnowledgeSyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantKnowledgeSyncLogs_PlantSpecies_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "PlantSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SpeciesId = table.Column<int>(type: "int", nullable: false),
                    NickName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EnvironmentNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plants_PlantSpecies_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "PlantSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlantSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpeciesId = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Author = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReliabilityLevel = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantSources_PlantSpecies_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "PlantSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantDiaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    DiaryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeatherNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnvironmentNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WateringNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FertilizerNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantDiaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantDiaries_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantSourceContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CleanText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Keywords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParsedJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParserType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ParserVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantSourceContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantSourceContents_PlantSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "PlantSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    DiaryId = table.Column<int>(type: "int", nullable: true),
                    AnalysisType = table.Column<int>(type: "int", nullable: false),
                    AnalysisScope = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PromptVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    InputSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HealthScore = table.Column<int>(type: "int", nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantAnalyses_PlantDiaries_DiaryId",
                        column: x => x.DiaryId,
                        principalTable: "PlantDiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlantAnalyses_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlantImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiaryId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantImages_PlantDiaries_DiaryId",
                        column: x => x.DiaryId,
                        principalTable: "PlantDiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantAnalysisJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    DiaryId = table.Column<int>(type: "int", nullable: true),
                    AnalysisId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AnalysisScope = table.Column<int>(type: "int", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantAnalysisJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantAnalysisJobs_PlantAnalyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "PlantAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlantAnalysisJobs_PlantDiaries_DiaryId",
                        column: x => x.DiaryId,
                        principalTable: "PlantDiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlantAnalysisJobs_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantAnalyses_DiaryId",
                table: "PlantAnalyses",
                column: "DiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantAnalyses_PlantId_CreatedAt",
                table: "PlantAnalyses",
                columns: new[] { "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlantAnalysisJobs_AnalysisId",
                table: "PlantAnalysisJobs",
                column: "AnalysisId",
                unique: true,
                filter: "[AnalysisId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlantAnalysisJobs_DiaryId",
                table: "PlantAnalysisJobs",
                column: "DiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantAnalysisJobs_PlantId",
                table: "PlantAnalysisJobs",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantAnalysisJobs_Status",
                table: "PlantAnalysisJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PlantDiaries_PlantId_DiaryDate",
                table: "PlantDiaries",
                columns: new[] { "PlantId", "DiaryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PlantImages_DiaryId",
                table: "PlantImages",
                column: "DiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantKnowledge_SpeciesId",
                table: "PlantKnowledge",
                column: "SpeciesId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlantKnowledgeSyncLogs_Provider",
                table: "PlantKnowledgeSyncLogs",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_PlantKnowledgeSyncLogs_SpeciesId",
                table: "PlantKnowledgeSyncLogs",
                column: "SpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_IsActive",
                table: "Plants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_SpeciesId",
                table: "Plants",
                column: "SpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantSourceContents_ContentHash",
                table: "PlantSourceContents",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_PlantSourceContents_SourceId",
                table: "PlantSourceContents",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantSources_ContentHash",
                table: "PlantSources",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_PlantSources_SpeciesId",
                table: "PlantSources",
                column: "SpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantSources_Url",
                table: "PlantSources",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_PlantSpecies_ChineseName",
                table: "PlantSpecies",
                column: "ChineseName");

            migrationBuilder.CreateIndex(
                name: "IX_PlantSpecies_CommonName",
                table: "PlantSpecies",
                column: "CommonName");

            migrationBuilder.CreateIndex(
                name: "IX_PlantSpecies_ScientificName",
                table: "PlantSpecies",
                column: "ScientificName");

            migrationBuilder.CreateIndex(
                name: "IX_PlantSpecies_TaxonId",
                table: "PlantSpecies",
                column: "TaxonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantAnalysisJobs");

            migrationBuilder.DropTable(
                name: "PlantImages");

            migrationBuilder.DropTable(
                name: "PlantKnowledge");

            migrationBuilder.DropTable(
                name: "PlantKnowledgeSyncLogs");

            migrationBuilder.DropTable(
                name: "PlantSourceContents");

            migrationBuilder.DropTable(
                name: "PlantAnalyses");

            migrationBuilder.DropTable(
                name: "PlantSources");

            migrationBuilder.DropTable(
                name: "PlantDiaries");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropTable(
                name: "PlantSpecies");
        }
    }
}

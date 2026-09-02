using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Midnight.EC.Plant.WEB.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantSourceSpeciesLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantSourceSpecies",
                columns: table => new
                {
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    SpeciesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantSourceSpecies", x => new { x.SourceId, x.SpeciesId });
                    table.ForeignKey(
                        name: "FK_PlantSourceSpecies_PlantSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "PlantSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlantSourceSpecies_PlantSpecies_SpeciesId",
                        column: x => x.SpeciesId,
                        principalTable: "PlantSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantSourceSpecies_SpeciesId",
                table: "PlantSourceSpecies",
                column: "SpeciesId");

            migrationBuilder.Sql("""
                INSERT INTO PlantSourceSpecies (SourceId, SpeciesId)
                SELECT Id, SpeciesId FROM PlantSources
                WHERE SpeciesId IS NOT NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantSourceSpecies");
        }
    }
}

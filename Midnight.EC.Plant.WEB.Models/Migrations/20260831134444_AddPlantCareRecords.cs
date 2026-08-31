using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Midnight.EC.Plant.WEB.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantCareRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantCareRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CareType = table.Column<int>(type: "int", nullable: false),
                    NumericValue = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantCareRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantCareRecords_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantCareRecords_CareType",
                table: "PlantCareRecords",
                column: "CareType");

            migrationBuilder.CreateIndex(
                name: "IX_PlantCareRecords_PlantId_RecordDate",
                table: "PlantCareRecords",
                columns: new[] { "PlantId", "RecordDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantCareRecords");
        }
    }
}

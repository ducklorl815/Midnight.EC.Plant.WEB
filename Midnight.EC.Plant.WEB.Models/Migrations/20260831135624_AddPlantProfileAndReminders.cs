using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Midnight.EC.Plant.WEB.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantProfileAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    WateringIntervalDays = table.Column<int>(type: "int", nullable: true),
                    FertilizingIntervalDays = table.Column<int>(type: "int", nullable: true),
                    TargetHumidityMin = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TargetHumidityMax = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TargetTemperatureMin = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TargetTemperatureMax = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PersonalCareNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantProfiles_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantReminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    ReminderType = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantReminders_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantProfiles_PlantId",
                table: "PlantProfiles",
                column: "PlantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlantReminders_DueDate",
                table: "PlantReminders",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_PlantReminders_PlantId_SourceKey",
                table: "PlantReminders",
                columns: new[] { "PlantId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlantReminders_PlantId_Status",
                table: "PlantReminders",
                columns: new[] { "PlantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantProfiles");

            migrationBuilder.DropTable(
                name: "PlantReminders");
        }
    }
}

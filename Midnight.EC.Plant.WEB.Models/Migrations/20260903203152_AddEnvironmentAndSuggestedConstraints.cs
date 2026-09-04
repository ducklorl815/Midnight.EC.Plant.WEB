using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Midnight.EC.Plant.WEB.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvironmentAndSuggestedConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActualLight",
                table: "PlantProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualPlacement",
                table: "PlantProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "PlantProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnvironmentMismatchAcknowledged",
                table: "PlantProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasRainCover",
                table: "PlantProfiles",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideCareTaboosJson",
                table: "PlantProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverrideSuggestedLight",
                table: "PlantProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubstrateType",
                table: "PlantProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WateringIntervalDetachedFromWiki",
                table: "PlantProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CareTaboosJson",
                table: "PlantKnowledge",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuggestedLight",
                table: "PlantKnowledge",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuggestedWateringIntervalDays",
                table: "PlantKnowledge",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualLight",
                table: "PlantProfiles");

            migrationBuilder.DropColumn(
                name: "ActualPlacement",
                table: "PlantProfiles");

            migrationBuilder.DropColumn(
                name: "City",
                table: "PlantProfiles");

            migrationBuilder.DropColumn(
                name: "EnvironmentMismatchAcknowledged",
                table: "PlantProfiles");

            migrationBuilder.DropColumn(
                name: "HasRainCover",
                table: "PlantProfiles");

            migrationBuilder.DropColumn(
                name: "OverrideCareTaboosJson",
                table: "PlantProfiles");

            migrationBuilder.DropColumn(
                name: "OverrideSuggestedLight",
                table: "PlantProfiles");

            migrationBuilder.DropColumn(
                name: "SubstrateType",
                table: "PlantProfiles");

            migrationBuilder.DropColumn(
                name: "WateringIntervalDetachedFromWiki",
                table: "PlantProfiles");

            migrationBuilder.DropColumn(
                name: "CareTaboosJson",
                table: "PlantKnowledge");

            migrationBuilder.DropColumn(
                name: "SuggestedLight",
                table: "PlantKnowledge");

            migrationBuilder.DropColumn(
                name: "SuggestedWateringIntervalDays",
                table: "PlantKnowledge");
        }
    }
}

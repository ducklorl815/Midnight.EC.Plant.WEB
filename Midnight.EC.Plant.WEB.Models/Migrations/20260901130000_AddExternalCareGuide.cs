using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Midnight.EC.Plant.WEB.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalCareGuide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalCareGuide",
                table: "PlantKnowledge",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalCareGuide",
                table: "PlantKnowledge");
        }
    }
}

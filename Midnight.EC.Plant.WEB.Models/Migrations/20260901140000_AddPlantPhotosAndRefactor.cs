using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Midnight.EC.Plant.WEB.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantPhotosAndRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlantId",
                table: "PlantImages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "PlantImages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCover",
                table: "PlantImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE pi SET pi.PlantId = pd.PlantId
                FROM PlantImages pi
                INNER JOIN PlantDiaries pd ON pi.DiaryId = pd.Id
                WHERE pi.PlantId IS NULL
                """);

            migrationBuilder.Sql("""
                UPDATE PlantImages SET PlantId = 0 WHERE PlantId IS NULL
                """);

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "PlantImages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DiaryId",
                table: "PlantImages",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_PlantImages_PlantId",
                table: "PlantImages",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantImages_PlantId_IsCover",
                table: "PlantImages",
                columns: new[] { "PlantId", "IsCover" });

            migrationBuilder.AddForeignKey(
                name: "FK_PlantImages_Plants_PlantId",
                table: "PlantImages",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<int>(
                name: "ImageId",
                table: "PlantAnalysisJobs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlantAnalysisJobs_ImageId",
                table: "PlantAnalysisJobs",
                column: "ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlantAnalysisJobs_PlantImages_ImageId",
                table: "PlantAnalysisJobs",
                column: "ImageId",
                principalTable: "PlantImages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<int>(
                name: "ImageId",
                table: "PlantAnalyses",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlantAnalysisJobs_PlantImages_ImageId",
                table: "PlantAnalysisJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_PlantImages_Plants_PlantId",
                table: "PlantImages");

            migrationBuilder.DropIndex(
                name: "IX_PlantAnalysisJobs_ImageId",
                table: "PlantAnalysisJobs");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "PlantAnalysisJobs");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "PlantAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_PlantImages_PlantId_IsCover",
                table: "PlantImages");

            migrationBuilder.DropIndex(
                name: "IX_PlantImages_PlantId",
                table: "PlantImages");

            migrationBuilder.DropColumn(
                name: "IsCover",
                table: "PlantImages");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "PlantImages");

            migrationBuilder.DropColumn(
                name: "PlantId",
                table: "PlantImages");

            migrationBuilder.AlterColumn<int>(
                name: "DiaryId",
                table: "PlantImages",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}

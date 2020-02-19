using Microsoft.EntityFrameworkCore.Migrations;

namespace METS.Context.Migrations.Project
{
    public partial class ImageSize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageH",
                table: "Projects",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImageW",
                table: "Projects",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageH",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ImageW",
                table: "Projects");
        }
    }
}

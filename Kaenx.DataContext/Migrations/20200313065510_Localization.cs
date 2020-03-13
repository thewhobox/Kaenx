using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations
{
    public partial class Localization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Text_EN",
                table: "AppComObjects",
                newName: "Text");

            migrationBuilder.RenameColumn(
                name: "Text_DE",
                table: "AppComObjects",
                newName: "FunctionText");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Text",
                table: "AppComObjects",
                newName: "Text_EN");

            migrationBuilder.RenameColumn(
                name: "FunctionText",
                table: "AppComObjects",
                newName: "Text_DE");

            migrationBuilder.AddColumn<string>(
                name: "FunctionText_DE",
                table: "AppComObjects",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunctionText_EN",
                table: "AppComObjects",
                maxLength: 100,
                nullable: true);
        }
    }
}

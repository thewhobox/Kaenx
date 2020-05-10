using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations
{
    public partial class Union : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UnionDefault",
                table: "AppParameters",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UnionId",
                table: "AppParameters",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnionDefault",
                table: "AppParameters");

            migrationBuilder.DropColumn(
                name: "UnionId",
                table: "AppParameters");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations
{
    public partial class LoadProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoadProcedure",
                table: "Applications",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoadProcedure",
                table: "Applications");
        }
    }
}

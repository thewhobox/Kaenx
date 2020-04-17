using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations.Local
{
    public partial class LocalInterface2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhAddr",
                table: "Interfaces",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhAddr",
                table: "Interfaces");
        }
    }
}

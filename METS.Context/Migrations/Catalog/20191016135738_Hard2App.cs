using Microsoft.EntityFrameworkCore.Migrations;

namespace METS.Context.Migrations.Catalog
{
    public partial class Hard2App : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Hardware2App",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Hardware2App",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Hardware2App",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Hardware2App");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Hardware2App");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Hardware2App");

            migrationBuilder.AddColumn<string>(
                name: "HardwareId",
                table: "Applications",
                maxLength: 100,
                nullable: true);
        }
    }
}

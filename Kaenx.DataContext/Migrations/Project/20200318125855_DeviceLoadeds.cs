using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations.Project
{
    public partial class DeviceLoadeds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LoadedApp",
                table: "LineDevices",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LoadedGA",
                table: "LineDevices",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LoadedPA",
                table: "LineDevices",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoadedApp",
                table: "LineDevices");

            migrationBuilder.DropColumn(
                name: "LoadedGA",
                table: "LineDevices");

            migrationBuilder.DropColumn(
                name: "LoadedPA",
                table: "LineDevices");
        }
    }
}

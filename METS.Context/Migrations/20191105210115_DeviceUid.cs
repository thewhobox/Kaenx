using Microsoft.EntityFrameworkCore.Migrations;

namespace METS.Context.Migrations
{
    public partial class DeviceUid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "LineDevices",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "LineDevices");
        }
    }
}

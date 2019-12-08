using Microsoft.EntityFrameworkCore.Migrations;

namespace METS.Context.Migrations
{
    public partial class Groups3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "GroupMiddle",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "GroupMain",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "GroupAddress",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "GroupMiddle");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "GroupMain");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "GroupAddress");
        }
    }
}

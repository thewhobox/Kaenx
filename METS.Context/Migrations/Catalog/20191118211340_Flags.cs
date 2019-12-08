using Microsoft.EntityFrameworkCore.Migrations;

namespace METS.Context.Migrations.Catalog
{
    public partial class Flags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Flag_ReadOnInit",
                table: "AppComObjects",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Flag_ReadOnInit",
                table: "AppComObjects");
        }
    }
}

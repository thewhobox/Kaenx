using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations
{
    public partial class SegmentType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AbsoluteSegmentId",
                table: "AppParameters",
                newName: "SegmentId");

            migrationBuilder.AddColumn<int>(
                name: "SegmentType",
                table: "AppParameters",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SegmentType",
                table: "AppParameters");

            migrationBuilder.RenameColumn(
                name: "SegmentId",
                table: "AppParameters",
                newName: "AbsoluteSegmentId");

            migrationBuilder.CreateTable(
                name: "Manufacturer",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    KnxManufacturerId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manufacturer", x => x.Id);
                });
        }
    }
}

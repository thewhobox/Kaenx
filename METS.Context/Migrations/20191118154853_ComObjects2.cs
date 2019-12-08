using Microsoft.EntityFrameworkCore.Migrations;

namespace METS.Context.Migrations
{
    public partial class ComObjects2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComObjects",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComId = table.Column<string>(nullable: true),
                    DeviceId = table.Column<int>(nullable: false),
                    Groups = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComObjects", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComObjects");

            migrationBuilder.AddColumn<string>(
                name: "ComObjects",
                table: "LineDevices",
                nullable: true);
        }
    }
}

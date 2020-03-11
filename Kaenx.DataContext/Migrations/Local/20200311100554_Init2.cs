using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations.Local
{
    public partial class Init2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalConnection");

            migrationBuilder.CreateTable(
                name: "ConnsCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    DbName = table.Column<string>(nullable: true),
                    DbUsername = table.Column<string>(nullable: true),
                    DbPassword = table.Column<string>(nullable: true),
                    DbHostname = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnsCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConnsProject",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    DbName = table.Column<string>(nullable: true),
                    DbUsername = table.Column<string>(nullable: true),
                    DbPassword = table.Column<string>(nullable: true),
                    DbHostname = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnsProject", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnsCatalog");

            migrationBuilder.DropTable(
                name: "ConnsProject");

            migrationBuilder.CreateTable(
                name: "LocalConnection",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DbHostname = table.Column<string>(nullable: true),
                    DbName = table.Column<string>(nullable: true),
                    DbPassword = table.Column<string>(nullable: true),
                    DbUsername = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalConnection", x => x.Id);
                });
        }
    }
}

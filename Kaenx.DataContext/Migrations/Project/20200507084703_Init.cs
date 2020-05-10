using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations.Project
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChangesParam",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<int>(nullable: false),
                    ParamId = table.Column<string>(nullable: true),
                    StateId = table.Column<int>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangesParam", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "LineDevices",
                columns: table => new
                {
                    UId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Id = table.Column<int>(nullable: false),
                    ParentId = table.Column<int>(nullable: false),
                    ProjectId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    DeviceId = table.Column<string>(nullable: true),
                    ApplicationId = table.Column<string>(nullable: true),
                    LoadedGA = table.Column<bool>(nullable: false),
                    LoadedApp = table.Column<bool>(nullable: false),
                    LoadedPA = table.Column<bool>(nullable: false),
                    Serial = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineDevices", x => x.UId);
                });

            migrationBuilder.CreateTable(
                name: "LinesMain",
                columns: table => new
                {
                    UId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Id = table.Column<int>(nullable: false),
                    ProjectId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    IsExpanded = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinesMain", x => x.UId);
                });

            migrationBuilder.CreateTable(
                name: "LinesMiddle",
                columns: table => new
                {
                    UId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Id = table.Column<int>(nullable: false),
                    ParentId = table.Column<int>(nullable: false),
                    ProjectId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    IsExpanded = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinesMiddle", x => x.UId);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Image = table.Column<byte[]>(nullable: true),
                    Area = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 50, nullable: true),
                    Description = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.Id);
                });


            if(migrationBuilder.ActiveProvider == "Pomelo.EntityFrameworkCore.MySql")
            {
                migrationBuilder.Sql("ALTER TABLE ChangesParam MODIFY Id INT(11) AUTO_INCREMENT;");
                migrationBuilder.Sql("ALTER TABLE ComObjects MODIFY Id INT(11) AUTO_INCREMENT;");
                migrationBuilder.Sql("ALTER TABLE LineDevices MODIFY UId INT(11) AUTO_INCREMENT;");
                migrationBuilder.Sql("ALTER TABLE LinesMain MODIFY UId INT(11) AUTO_INCREMENT;");
                migrationBuilder.Sql("ALTER TABLE LinesMiddle MODIFY UId INT(11) AUTO_INCREMENT;");
                migrationBuilder.Sql("ALTER TABLE Projects MODIFY Id INT(11) AUTO_INCREMENT;");
                migrationBuilder.Sql("ALTER TABLE States MODIFY Id INT(11) AUTO_INCREMENT;");
            }
            Debug.WriteLine(migrationBuilder.ActiveProvider);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangesParam");

            migrationBuilder.DropTable(
                name: "ComObjects");

            migrationBuilder.DropTable(
                name: "LineDevices");

            migrationBuilder.DropTable(
                name: "LinesMain");

            migrationBuilder.DropTable(
                name: "LinesMiddle");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "States");
        }
    }
}

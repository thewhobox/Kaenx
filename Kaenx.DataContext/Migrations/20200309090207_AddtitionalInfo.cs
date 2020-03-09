using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations
{
    public partial class AddtitionalInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppAdditionals",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    LoadProcedures = table.Column<byte[]>(nullable: true),
                    Dynamic = table.Column<byte[]>(nullable: true),
                    ParameterAll = table.Column<byte[]>(nullable: true),
                    ParameterDefault = table.Column<byte[]>(nullable: true),
                    ComsAll = table.Column<byte[]>(nullable: true),
                    ComsDefault = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAdditionals", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppAdditionals");
        }
    }
}

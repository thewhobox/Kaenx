using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations.Project
{
    public partial class Serial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Serial",
                table: "LineDevices",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Serial",
                table: "LineDevices");
        }
    }
}

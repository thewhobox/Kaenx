using Microsoft.EntityFrameworkCore.Migrations;

namespace METS.Context.Migrations
{
    public partial class Groups2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GroupMiddle",
                table: "GroupMiddle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GroupMain",
                table: "GroupMain");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GroupAddress",
                table: "GroupAddress");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "GroupMiddle",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "UId",
                table: "GroupMiddle",
                nullable: false,
                oldClrType: typeof(int))
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "GroupMain",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "UId",
                table: "GroupMain",
                nullable: false,
                oldClrType: typeof(int))
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "GroupAddress",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "UId",
                table: "GroupAddress",
                nullable: false,
                oldClrType: typeof(int))
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GroupMiddle",
                table: "GroupMiddle",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GroupMain",
                table: "GroupMain",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GroupAddress",
                table: "GroupAddress",
                column: "Id");
        }
    }
}

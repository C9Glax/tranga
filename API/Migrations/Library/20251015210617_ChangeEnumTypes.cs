using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations.Library
{
    /// <inheritdoc />
    public partial class ChangeEnumTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "LibraryType",
                table: "LibraryConnectors",
                type: "integer",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "smallint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "LibraryType",
                table: "LibraryConnectors",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}

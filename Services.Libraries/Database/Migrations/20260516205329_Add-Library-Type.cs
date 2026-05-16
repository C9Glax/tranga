using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.Libraries.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LibraryType",
                table: "Libraries",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LibraryType",
                table: "Libraries");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.Libraries.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Libraries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Libraries");
        }
    }
}

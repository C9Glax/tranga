using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.Libraries.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "LibraryServices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Username",
                table: "LibraryServices");
        }
    }
}

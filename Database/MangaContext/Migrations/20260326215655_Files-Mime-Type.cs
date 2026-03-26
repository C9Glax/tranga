using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class FilesMimeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "Files",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "Files");
        }
    }
}

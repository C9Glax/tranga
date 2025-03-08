using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dev0803252 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AltTitleIds",
                table: "Manga");

            migrationBuilder.DropColumn(
                name: "AuthorIds",
                table: "Manga");

            migrationBuilder.DropColumn(
                name: "LinkIds",
                table: "Manga");

            migrationBuilder.DropColumn(
                name: "TagIds",
                table: "Manga");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "AltTitleIds",
                table: "Manga",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "AuthorIds",
                table: "Manga",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "LinkIds",
                table: "Manga",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "TagIds",
                table: "Manga",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }
    }
}

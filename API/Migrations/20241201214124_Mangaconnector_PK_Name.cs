using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class Mangaconnector_PK_Name : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Manga_MangaConnectors_MangaConnectorId",
                table: "Manga");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MangaConnectors",
                table: "MangaConnectors");

            migrationBuilder.DropIndex(
                name: "IX_Manga_MangaConnectorId",
                table: "Manga");

            migrationBuilder.DropColumn(
                name: "MangaConnectorId",
                table: "MangaConnectors");

            migrationBuilder.DropColumn(
                name: "MangaConnectorId",
                table: "Manga");

            migrationBuilder.AddColumn<string>(
                name: "MangaConnectorName",
                table: "Manga",
                type: "character varying(32)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MangaConnectors",
                table: "MangaConnectors",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Manga_MangaConnectorName",
                table: "Manga",
                column: "MangaConnectorName");

            migrationBuilder.AddForeignKey(
                name: "FK_Manga_MangaConnectors_MangaConnectorName",
                table: "Manga",
                column: "MangaConnectorName",
                principalTable: "MangaConnectors",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Manga_MangaConnectors_MangaConnectorName",
                table: "Manga");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MangaConnectors",
                table: "MangaConnectors");

            migrationBuilder.DropIndex(
                name: "IX_Manga_MangaConnectorName",
                table: "Manga");

            migrationBuilder.DropColumn(
                name: "MangaConnectorName",
                table: "Manga");

            migrationBuilder.AddColumn<string>(
                name: "MangaConnectorId",
                table: "MangaConnectors",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MangaConnectorId",
                table: "Manga",
                type: "character varying(64)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MangaConnectors",
                table: "MangaConnectors",
                column: "MangaConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Manga_MangaConnectorId",
                table: "Manga",
                column: "MangaConnectorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Manga_MangaConnectors_MangaConnectorId",
                table: "Manga",
                column: "MangaConnectorId",
                principalTable: "MangaConnectors",
                principalColumn: "MangaConnectorId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

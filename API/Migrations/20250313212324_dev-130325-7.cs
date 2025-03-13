using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dev1303257 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AltTitles_Manga_MangaId",
                table: "AltTitles");

            migrationBuilder.DropForeignKey(
                name: "FK_Link_Manga_MangaId",
                table: "Link");

            migrationBuilder.AddForeignKey(
                name: "FK_AltTitles_Manga_MangaId",
                table: "AltTitles",
                column: "MangaId",
                principalTable: "Manga",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Link_Manga_MangaId",
                table: "Link",
                column: "MangaId",
                principalTable: "Manga",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AltTitles_Manga_MangaId",
                table: "AltTitles");

            migrationBuilder.DropForeignKey(
                name: "FK_Link_Manga_MangaId",
                table: "Link");

            migrationBuilder.AddForeignKey(
                name: "FK_AltTitles_Manga_MangaId",
                table: "AltTitles",
                column: "MangaId",
                principalTable: "Manga",
                principalColumn: "MangaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Link_Manga_MangaId",
                table: "Link",
                column: "MangaId",
                principalTable: "Manga",
                principalColumn: "MangaId");
        }
    }
}

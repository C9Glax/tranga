using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dev0703252 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DownloadMangaCoverJob_MangaId",
                table: "Jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_DownloadMangaCoverJob_MangaId",
                table: "Jobs",
                column: "DownloadMangaCoverJob_MangaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Manga_DownloadMangaCoverJob_MangaId",
                table: "Jobs",
                column: "DownloadMangaCoverJob_MangaId",
                principalTable: "Manga",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Manga_DownloadMangaCoverJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_DownloadMangaCoverJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DownloadMangaCoverJob_MangaId",
                table: "Jobs");
        }
    }
}

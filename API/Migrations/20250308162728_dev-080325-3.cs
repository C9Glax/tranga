using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dev0803253 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Manga_DownloadMangaCoverJob_MangaId",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "DownloadMangaCoverJob_MangaId",
                table: "Jobs",
                newName: "UpdateFilesDownloadedJob_MangaId");

            migrationBuilder.RenameIndex(
                name: "IX_Jobs_DownloadMangaCoverJob_MangaId",
                table: "Jobs",
                newName: "IX_Jobs_UpdateFilesDownloadedJob_MangaId");

            migrationBuilder.AddColumn<string>(
                name: "DownloadAvailableChaptersJob_MangaId",
                table: "Jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetrieveChaptersJob_MangaId",
                table: "Jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs",
                column: "DownloadAvailableChaptersJob_MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_RetrieveChaptersJob_MangaId",
                table: "Jobs",
                column: "RetrieveChaptersJob_MangaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Manga_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs",
                column: "DownloadAvailableChaptersJob_MangaId",
                principalTable: "Manga",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Manga_RetrieveChaptersJob_MangaId",
                table: "Jobs",
                column: "RetrieveChaptersJob_MangaId",
                principalTable: "Manga",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Manga_UpdateFilesDownloadedJob_MangaId",
                table: "Jobs",
                column: "UpdateFilesDownloadedJob_MangaId",
                principalTable: "Manga",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Manga_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Manga_RetrieveChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Manga_UpdateFilesDownloadedJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_RetrieveChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DownloadAvailableChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RetrieveChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "UpdateFilesDownloadedJob_MangaId",
                table: "Jobs",
                newName: "DownloadMangaCoverJob_MangaId");

            migrationBuilder.RenameIndex(
                name: "IX_Jobs_UpdateFilesDownloadedJob_MangaId",
                table: "Jobs",
                newName: "IX_Jobs_DownloadMangaCoverJob_MangaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Manga_DownloadMangaCoverJob_MangaId",
                table: "Jobs",
                column: "DownloadMangaCoverJob_MangaId",
                principalTable: "Manga",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

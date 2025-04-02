using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dev0104254 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Chapters_ChapterId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_RetrieveChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_UpdateFilesDownloadedJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_ChapterId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_MangaId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_RetrieveChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_UpdateFilesDownloadedJob_MangaId",
                table: "Jobs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ChapterId",
                table: "Jobs",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs",
                column: "DownloadAvailableChaptersJob_MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_MangaId",
                table: "Jobs",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_RetrieveChaptersJob_MangaId",
                table: "Jobs",
                column: "RetrieveChaptersJob_MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_UpdateFilesDownloadedJob_MangaId",
                table: "Jobs",
                column: "UpdateFilesDownloadedJob_MangaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Chapters_ChapterId",
                table: "Jobs",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "ChapterId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs",
                column: "DownloadAvailableChaptersJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_MangaId",
                table: "Jobs",
                column: "MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_RetrieveChaptersJob_MangaId",
                table: "Jobs",
                column: "RetrieveChaptersJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_UpdateFilesDownloadedJob_MangaId",
                table: "Jobs",
                column: "UpdateFilesDownloadedJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

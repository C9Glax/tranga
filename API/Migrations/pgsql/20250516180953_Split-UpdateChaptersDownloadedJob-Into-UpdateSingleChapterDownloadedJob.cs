using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations.pgsql
{
    /// <inheritdoc />
    public partial class SplitUpdateChaptersDownloadedJobIntoUpdateSingleChapterDownloadedJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_UpdateFilesDownloadedJob_MangaId",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "UpdateFilesDownloadedJob_MangaId",
                table: "Jobs",
                newName: "UpdateChaptersDownloadedJob_MangaId");

            migrationBuilder.RenameIndex(
                name: "IX_Jobs_UpdateFilesDownloadedJob_MangaId",
                table: "Jobs",
                newName: "IX_Jobs_UpdateChaptersDownloadedJob_MangaId");

            migrationBuilder.AddColumn<string>(
                name: "UpdateSingleChapterDownloadedJob_ChapterId",
                table: "Jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_UpdateSingleChapterDownloadedJob_ChapterId",
                table: "Jobs",
                column: "UpdateSingleChapterDownloadedJob_ChapterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Chapters_UpdateSingleChapterDownloadedJob_ChapterId",
                table: "Jobs",
                column: "UpdateSingleChapterDownloadedJob_ChapterId",
                principalTable: "Chapters",
                principalColumn: "ChapterId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_UpdateChaptersDownloadedJob_MangaId",
                table: "Jobs",
                column: "UpdateChaptersDownloadedJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Chapters_UpdateSingleChapterDownloadedJob_ChapterId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_UpdateChaptersDownloadedJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_UpdateSingleChapterDownloadedJob_ChapterId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "UpdateSingleChapterDownloadedJob_ChapterId",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "UpdateChaptersDownloadedJob_MangaId",
                table: "Jobs",
                newName: "UpdateFilesDownloadedJob_MangaId");

            migrationBuilder.RenameIndex(
                name: "IX_Jobs_UpdateChaptersDownloadedJob_MangaId",
                table: "Jobs",
                newName: "IX_Jobs_UpdateFilesDownloadedJob_MangaId");

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

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations.pgsql
{
    /// <inheritdoc />
    public partial class RemoveUpdateSingleChapterDownloadedJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Chapters_UpdateSingleChapterDownloadedJob_ChapterId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_UpdateSingleChapterDownloadedJob_ChapterId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "UpdateSingleChapterDownloadedJob_ChapterId",
                table: "Jobs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}

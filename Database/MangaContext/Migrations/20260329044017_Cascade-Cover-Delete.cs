using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class CascadeCoverDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DbDownloadLink_Files_CoverId",
                table: "DbDownloadLink");

            migrationBuilder.DropForeignKey(
                name: "FK_DbMetadataLink_Files_CoverId",
                table: "DbMetadataLink");

            migrationBuilder.AddForeignKey(
                name: "FK_DbDownloadLink_Files_CoverId",
                table: "DbDownloadLink",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DbMetadataLink_Files_CoverId",
                table: "DbMetadataLink",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DbDownloadLink_Files_CoverId",
                table: "DbDownloadLink");

            migrationBuilder.DropForeignKey(
                name: "FK_DbMetadataLink_Files_CoverId",
                table: "DbMetadataLink");

            migrationBuilder.AddForeignKey(
                name: "FK_DbDownloadLink_Files_CoverId",
                table: "DbDownloadLink",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DbMetadataLink_Files_CoverId",
                table: "DbMetadataLink",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "Id");
        }
    }
}

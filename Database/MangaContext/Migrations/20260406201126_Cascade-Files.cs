using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class CascadeFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DownloadSources_Files_CoverId",
                table: "DownloadSources");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataSources_Files_CoverId",
                table: "MetadataSources");

            migrationBuilder.AddForeignKey(
                name: "FK_DownloadSources_Files_CoverId",
                table: "DownloadSources",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "FileId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataSources_Files_CoverId",
                table: "MetadataSources",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "FileId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DownloadSources_Files_CoverId",
                table: "DownloadSources");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataSources_Files_CoverId",
                table: "MetadataSources");

            migrationBuilder.AddForeignKey(
                name: "FK_DownloadSources_Files_CoverId",
                table: "DownloadSources",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataSources_Files_CoverId",
                table: "MetadataSources",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "FileId");
        }
    }
}

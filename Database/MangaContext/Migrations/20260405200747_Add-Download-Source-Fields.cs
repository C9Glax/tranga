using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadSourceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CoverId",
                table: "MangaDownloadSources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "MangaDownloadSources",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Series",
                table: "MangaDownloadSources",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "MangaDownloadSources",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DbMangaDownloadSourceCoverId",
                table: "Files",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_MangaDownloadSources_CoverId",
                table: "MangaDownloadSources",
                column: "CoverId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_DbMangaDownloadSourceCoverId",
                table: "Files",
                column: "DbMangaDownloadSourceCoverId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Files_MangaDownloadSources_DbMangaDownloadSourceCoverId",
                table: "Files",
                column: "DbMangaDownloadSourceCoverId",
                principalTable: "MangaDownloadSources",
                principalColumn: "CoverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_MangaDownloadSources_DbMangaDownloadSourceCoverId",
                table: "Files");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_MangaDownloadSources_CoverId",
                table: "MangaDownloadSources");

            migrationBuilder.DropIndex(
                name: "IX_Files_DbMangaDownloadSourceCoverId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "CoverId",
                table: "MangaDownloadSources");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "MangaDownloadSources");

            migrationBuilder.DropColumn(
                name: "Series",
                table: "MangaDownloadSources");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "MangaDownloadSources");

            migrationBuilder.DropColumn(
                name: "DbMangaDownloadSourceCoverId",
                table: "Files");
        }
    }
}

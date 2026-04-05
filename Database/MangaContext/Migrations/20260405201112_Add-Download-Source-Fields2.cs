using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadSourceFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_MangaDownloadSources_DbMangaDownloadSourceCoverId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_Files_MetadataSources_FileId",
                table: "Files");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_MetadataSources_CoverId",
                table: "MetadataSources");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_MangaDownloadSources_CoverId",
                table: "MangaDownloadSources");

            migrationBuilder.DropIndex(
                name: "IX_Files_DbMangaDownloadSourceCoverId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "DbMangaDownloadSourceCoverId",
                table: "Files");

            migrationBuilder.AlterColumn<Guid>(
                name: "CoverId",
                table: "MetadataSources",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "CoverId",
                table: "MangaDownloadSources",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataSources_CoverId",
                table: "MetadataSources",
                column: "CoverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MangaDownloadSources_CoverId",
                table: "MangaDownloadSources",
                column: "CoverId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MangaDownloadSources_Files_CoverId",
                table: "MangaDownloadSources",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MangaDownloadSources_Files_CoverId",
                table: "MangaDownloadSources");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataSources_Files_CoverId",
                table: "MetadataSources");

            migrationBuilder.DropIndex(
                name: "IX_MetadataSources_CoverId",
                table: "MetadataSources");

            migrationBuilder.DropIndex(
                name: "IX_MangaDownloadSources_CoverId",
                table: "MangaDownloadSources");

            migrationBuilder.AlterColumn<Guid>(
                name: "CoverId",
                table: "MetadataSources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CoverId",
                table: "MangaDownloadSources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DbMangaDownloadSourceCoverId",
                table: "Files",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_MetadataSources_CoverId",
                table: "MetadataSources",
                column: "CoverId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Files_MetadataSources_FileId",
                table: "Files",
                column: "FileId",
                principalTable: "MetadataSources",
                principalColumn: "CoverId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

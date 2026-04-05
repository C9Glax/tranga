using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class manytomanymangadownloadsource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MangaDownloadSources_Files_CoverId",
                table: "MangaDownloadSources");

            migrationBuilder.DropIndex(
                name: "IX_MangaDownloadSources_CoverId",
                table: "MangaDownloadSources");

            migrationBuilder.DropColumn(
                name: "CoverId",
                table: "MangaDownloadSources");

            migrationBuilder.DropColumn(
                name: "Identifier",
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
                name: "Url",
                table: "MangaDownloadSources");

            migrationBuilder.RenameColumn(
                name: "DownloadExtension",
                table: "MangaDownloadSources",
                newName: "DownloadSourceId");

            migrationBuilder.AddColumn<bool>(
                name: "Matched",
                table: "MangaDownloadSources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DownloadSources",
                columns: table => new
                {
                    DownloadId = table.Column<Guid>(type: "uuid", nullable: false),
                    DownloadExtension = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    Series = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Summary = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true),
                    CoverId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadSources", x => x.DownloadId);
                    table.ForeignKey(
                        name: "FK_DownloadSources_Files_CoverId",
                        column: x => x.CoverId,
                        principalTable: "Files",
                        principalColumn: "FileId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MangaDownloadSources_DownloadSourceId",
                table: "MangaDownloadSources",
                column: "DownloadSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadSources_CoverId",
                table: "DownloadSources",
                column: "CoverId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MangaDownloadSources_DownloadSources_DownloadSourceId",
                table: "MangaDownloadSources",
                column: "DownloadSourceId",
                principalTable: "DownloadSources",
                principalColumn: "DownloadId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MangaDownloadSources_DownloadSources_DownloadSourceId",
                table: "MangaDownloadSources");

            migrationBuilder.DropTable(
                name: "DownloadSources");

            migrationBuilder.DropIndex(
                name: "IX_MangaDownloadSources_DownloadSourceId",
                table: "MangaDownloadSources");

            migrationBuilder.DropColumn(
                name: "Matched",
                table: "MangaDownloadSources");

            migrationBuilder.RenameColumn(
                name: "DownloadSourceId",
                table: "MangaDownloadSources",
                newName: "DownloadExtension");

            migrationBuilder.AddColumn<Guid>(
                name: "CoverId",
                table: "MangaDownloadSources",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "MangaDownloadSources",
                type: "text",
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "MangaDownloadSources",
                type: "text",
                nullable: true);

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
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Manga",
                columns: table => new
                {
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MangaUpdatesSeriesId = table.Column<long>(type: "bigint", nullable: true),
                    CoverImageBase64 = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    Authors = table.Column<string[]>(type: "text[]", nullable: true),
                    Artists = table.Column<string[]>(type: "text[]", nullable: true),
                    Genre = table.Column<string[]>(type: "text[]", nullable: true),
                    Tags = table.Column<string[]>(type: "text[]", nullable: true),
                    AgeRating = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manga", x => x.MangaId);
                });

            migrationBuilder.CreateTable(
                name: "Chapter",
                columns: table => new
                {
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Volume = table.Column<string>(type: "text", nullable: true),
                    Chapter = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapter", x => x.ChapterId);
                    table.ForeignKey(
                        name: "FK_Chapter_Manga_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Manga",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaDownloadExtensionIds",
                columns: table => new
                {
                    ParentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    ExtensionIdentifier = table.Column<Guid>(type: "uuid", nullable: false),
                    DbMangaMangaId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaDownloadExtensionIds", x => new { x.ParentId, x.Identifier });
                    table.ForeignKey(
                        name: "FK_MangaDownloadExtensionIds_Manga_DbMangaMangaId",
                        column: x => x.DbMangaMangaId,
                        principalTable: "Manga",
                        principalColumn: "MangaId");
                    table.ForeignKey(
                        name: "FK_MangaDownloadExtensionIds_Manga_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Manga",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChapterDownloadExtensionIds",
                columns: table => new
                {
                    ParentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    ExtensionIdentifier = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterDownloadExtensionIds", x => new { x.ParentId, x.Identifier });
                    table.ForeignKey(
                        name: "FK_ChapterDownloadExtensionIds_Chapter_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Chapter",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chapter_MangaId",
                table: "Chapter",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaDownloadExtensionIds_DbMangaMangaId",
                table: "MangaDownloadExtensionIds",
                column: "DbMangaMangaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChapterDownloadExtensionIds");

            migrationBuilder.DropTable(
                name: "MangaDownloadExtensionIds");

            migrationBuilder.DropTable(
                name: "Chapter");

            migrationBuilder.DropTable(
                name: "Manga");
        }
    }
}

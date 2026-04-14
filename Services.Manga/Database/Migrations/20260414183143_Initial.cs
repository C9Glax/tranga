using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.Manga.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DbPerson",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbPerson", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.FileId);
                });

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Genre = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Genre);
                });

            migrationBuilder.CreateTable(
                name: "Mangas",
                columns: table => new
                {
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Monitored = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mangas", x => x.MangaId);
                });

            migrationBuilder.CreateTable(
                name: "DownloadLinks",
                columns: table => new
                {
                    DownloadLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    DownloadExtension = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    Series = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Summary = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true),
                    CoverId = table.Column<Guid>(type: "uuid", nullable: true),
                    NSFW = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadLinks", x => x.DownloadLinkId);
                    table.ForeignKey(
                        name: "FK_DownloadLinks_Files_CoverId",
                        column: x => x.CoverId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetadataEntries",
                columns: table => new
                {
                    MetadataId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetadataExtension = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    Series = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Summary = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    ChaptersNumber = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    CoverId = table.Column<Guid>(type: "uuid", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true),
                    NSFW = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataEntries", x => x.MetadataId);
                    table.ForeignKey(
                        name: "FK_MetadataEntries_Files_CoverId",
                        column: x => x.CoverId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Volume = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ReleaseDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.ChapterId);
                    table.ForeignKey(
                        name: "FK_Chapters_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaDownloadLinks",
                columns: table => new
                {
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DownloadLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Matched = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaDownloadLinks", x => new { x.MangaId, x.DownloadLinkId });
                    table.ForeignKey(
                        name: "FK_MangaDownloadLinks_DownloadLinks_DownloadLinkId",
                        column: x => x.DownloadLinkId,
                        principalTable: "DownloadLinks",
                        principalColumn: "DownloadLinkId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaDownloadLinks_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DbMangaArtists",
                columns: table => new
                {
                    MetadataId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistId = table.Column<string>(type: "character varying(128)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbMangaArtists", x => new { x.ArtistId, x.MetadataId });
                    table.ForeignKey(
                        name: "FK_DbMangaArtists_DbPerson_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "DbPerson",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DbMangaArtists_MetadataEntries_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "MetadataEntries",
                        principalColumn: "MetadataId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DbMangaAuthors",
                columns: table => new
                {
                    MetadataId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "character varying(128)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbMangaAuthors", x => new { x.AuthorId, x.MetadataId });
                    table.ForeignKey(
                        name: "FK_DbMangaAuthors_DbPerson_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "DbPerson",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DbMangaAuthors_MetadataEntries_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "MetadataEntries",
                        principalColumn: "MetadataId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DbMangaGenres",
                columns: table => new
                {
                    MetadataId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenreId = table.Column<string>(type: "character varying(128)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbMangaGenres", x => new { x.MetadataId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_DbMangaGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Genre",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DbMangaGenres_MetadataEntries_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "MetadataEntries",
                        principalColumn: "MetadataId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaMetadataEntries",
                columns: table => new
                {
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetadataId = table.Column<Guid>(type: "uuid", nullable: false),
                    Chosen = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaMetadataEntries", x => new { x.MangaId, x.MetadataId });
                    table.ForeignKey(
                        name: "FK_MangaMetadataEntries_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaMetadataEntries_MetadataEntries_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "MetadataEntries",
                        principalColumn: "MetadataId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChapterDownloadLinks",
                columns: table => new
                {
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    DownloadExtension = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterDownloadLinks", x => new { x.ChapterId, x.DownloadExtension });
                    table.ForeignKey(
                        name: "FK_ChapterDownloadLinks_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChapterDownloadLinks_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "FileId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterDownloadLinks_FileId",
                table: "ChapterDownloadLinks",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_MangaId",
                table: "Chapters",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_DbMangaArtists_MetadataId",
                table: "DbMangaArtists",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_DbMangaAuthors_MetadataId",
                table: "DbMangaAuthors",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_DbMangaGenres_GenreId",
                table: "DbMangaGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadLinks_CoverId",
                table: "DownloadLinks",
                column: "CoverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_File_Path_Name",
                table: "Files",
                columns: new[] { "Path", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_MangaDownloadLinks_DownloadLinkId",
                table: "MangaDownloadLinks",
                column: "DownloadLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaMetadataEntries_MetadataId",
                table: "MangaMetadataEntries",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_Extension_Identifier",
                table: "MetadataEntries",
                columns: new[] { "MetadataExtension", "Identifier" });

            migrationBuilder.CreateIndex(
                name: "IX_MetadataEntries_CoverId",
                table: "MetadataEntries",
                column: "CoverId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChapterDownloadLinks");

            migrationBuilder.DropTable(
                name: "DbMangaArtists");

            migrationBuilder.DropTable(
                name: "DbMangaAuthors");

            migrationBuilder.DropTable(
                name: "DbMangaGenres");

            migrationBuilder.DropTable(
                name: "MangaDownloadLinks");

            migrationBuilder.DropTable(
                name: "MangaMetadataEntries");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "DbPerson");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "DownloadLinks");

            migrationBuilder.DropTable(
                name: "MetadataEntries");

            migrationBuilder.DropTable(
                name: "Mangas");

            migrationBuilder.DropTable(
                name: "Files");
        }
    }
}

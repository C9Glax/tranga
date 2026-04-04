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
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mangas", x => x.MangaId);
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
                name: "MangaDownloadSources",
                columns: table => new
                {
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DownloadExtension = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaDownloadSources", x => new { x.MangaId, x.DownloadExtension });
                    table.ForeignKey(
                        name: "FK_MangaDownloadSources_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetadataSources",
                columns: table => new
                {
                    MetadataId = table.Column<Guid>(type: "uuid", nullable: false),
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    MetadataExtension = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    Series = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Summary = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    ChaptersNumber = table.Column<int>(type: "integer", nullable: true),
                    CoverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataSources", x => x.MetadataId);
                    table.UniqueConstraint("AK_MetadataSources_CoverId", x => x.CoverId);
                    table.ForeignKey(
                        name: "FK_MetadataSources_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DbMangaArtists",
                columns: table => new
                {
                    MetadataSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistId = table.Column<string>(type: "character varying(128)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbMangaArtists", x => new { x.ArtistId, x.MetadataSourceId });
                    table.ForeignKey(
                        name: "FK_DbMangaArtists_DbPerson_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "DbPerson",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DbMangaArtists_MetadataSources_MetadataSourceId",
                        column: x => x.MetadataSourceId,
                        principalTable: "MetadataSources",
                        principalColumn: "MetadataId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DbMangaAuthors",
                columns: table => new
                {
                    MetadataSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "character varying(128)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbMangaAuthors", x => new { x.AuthorId, x.MetadataSourceId });
                    table.ForeignKey(
                        name: "FK_DbMangaAuthors_DbPerson_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "DbPerson",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DbMangaAuthors_MetadataSources_MetadataSourceId",
                        column: x => x.MetadataSourceId,
                        principalTable: "MetadataSources",
                        principalColumn: "MetadataId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DbMangaGenres",
                columns: table => new
                {
                    MetadataSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenreId = table.Column<string>(type: "character varying(128)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbMangaGenres", x => new { x.MetadataSourceId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_DbMangaGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Genre",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DbMangaGenres_MetadataSources_MetadataSourceId",
                        column: x => x.MetadataSourceId,
                        principalTable: "MetadataSources",
                        principalColumn: "MetadataId",
                        onDelete: ReferentialAction.Cascade);
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
                    table.ForeignKey(
                        name: "FK_Files_MetadataSources_FileId",
                        column: x => x.FileId,
                        principalTable: "MetadataSources",
                        principalColumn: "CoverId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChapterDownloadSources",
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
                    table.PrimaryKey("PK_ChapterDownloadSources", x => new { x.ChapterId, x.DownloadExtension });
                    table.ForeignKey(
                        name: "FK_ChapterDownloadSources_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChapterDownloadSources_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "FileId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterDownloadSources_FileId",
                table: "ChapterDownloadSources",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_MangaId",
                table: "Chapters",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_DbMangaArtists_MetadataSourceId",
                table: "DbMangaArtists",
                column: "MetadataSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_DbMangaAuthors_MetadataSourceId",
                table: "DbMangaAuthors",
                column: "MetadataSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_DbMangaGenres_GenreId",
                table: "DbMangaGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_File_Path_Name",
                table: "Files",
                columns: new[] { "Path", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_Extension_Identifier",
                table: "MetadataSources",
                columns: new[] { "MetadataExtension", "Identifier" });

            migrationBuilder.CreateIndex(
                name: "IX_MetadataSources_MangaId",
                table: "MetadataSources",
                column: "MangaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChapterDownloadSources");

            migrationBuilder.DropTable(
                name: "DbMangaArtists");

            migrationBuilder.DropTable(
                name: "DbMangaAuthors");

            migrationBuilder.DropTable(
                name: "DbMangaGenres");

            migrationBuilder.DropTable(
                name: "MangaDownloadSources");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "DbPerson");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "MetadataSources");

            migrationBuilder.DropTable(
                name: "Mangas");
        }
    }
}

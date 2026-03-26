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
                name: "DbFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbFile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Genre",
                columns: table => new
                {
                    Genre = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genre", x => x.Genre);
                });

            migrationBuilder.CreateTable(
                name: "Mangas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Series = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Monitor = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mangas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "DbDownloadLink",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DownloadExtensionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbDownloadLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbDownloadLink_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DbMetadataLink",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetadataExtensionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    CoverId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Rating = table.Column<byte>(type: "smallint", nullable: true),
                    Demographic = table.Column<byte>(type: "smallint", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbMetadataLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbMetadataLink_DbFile_CoverId",
                        column: x => x.CoverId,
                        principalTable: "DbFile",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DbMetadataLink_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DownloadLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    DownloadExtensionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    Volume = table.Column<string>(type: "text", nullable: true),
                    Chapter = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true),
                    Download = table.Column<bool>(type: "boolean", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapters_DbDownloadLink_DownloadLinkId",
                        column: x => x.DownloadLinkId,
                        principalTable: "DbDownloadLink",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Chapters_DbFile_FileId",
                        column: x => x.FileId,
                        principalTable: "DbFile",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MangaArtists",
                columns: table => new
                {
                    ArtistsName = table.Column<string>(type: "character varying(128)", nullable: false),
                    DbMetadataLink1Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaArtists", x => new { x.ArtistsName, x.DbMetadataLink1Id });
                    table.ForeignKey(
                        name: "FK_MangaArtists_DbMetadataLink_DbMetadataLink1Id",
                        column: x => x.DbMetadataLink1Id,
                        principalTable: "DbMetadataLink",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaArtists_People_ArtistsName",
                        column: x => x.ArtistsName,
                        principalTable: "People",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaAuthors",
                columns: table => new
                {
                    AuthorsName = table.Column<string>(type: "character varying(128)", nullable: false),
                    DbMetadataLinkId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaAuthors", x => new { x.AuthorsName, x.DbMetadataLinkId });
                    table.ForeignKey(
                        name: "FK_MangaAuthors_DbMetadataLink_DbMetadataLinkId",
                        column: x => x.DbMetadataLinkId,
                        principalTable: "DbMetadataLink",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaAuthors_People_AuthorsName",
                        column: x => x.AuthorsName,
                        principalTable: "People",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaGenres",
                columns: table => new
                {
                    DbMetadataLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenresGenre = table.Column<string>(type: "character varying(128)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaGenres", x => new { x.DbMetadataLinkId, x.GenresGenre });
                    table.ForeignKey(
                        name: "FK_MangaGenres_DbMetadataLink_DbMetadataLinkId",
                        column: x => x.DbMetadataLinkId,
                        principalTable: "DbMetadataLink",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaGenres_Genre_GenresGenre",
                        column: x => x.GenresGenre,
                        principalTable: "Genre",
                        principalColumn: "Genre",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_DownloadExtensionId_Identifier",
                table: "Chapters",
                columns: new[] { "DownloadExtensionId", "Identifier" });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_DownloadLinkId",
                table: "Chapters",
                column: "DownloadLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_FileId",
                table: "Chapters",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DbDownloadLink_DownloadExtensionId_Identifier",
                table: "DbDownloadLink",
                columns: new[] { "DownloadExtensionId", "Identifier" });

            migrationBuilder.CreateIndex(
                name: "IX_DbDownloadLink_MangaId",
                table: "DbDownloadLink",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_DbMetadataLink_CoverId",
                table: "DbMetadataLink",
                column: "CoverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DbMetadataLink_MangaId",
                table: "DbMetadataLink",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_DbMetadataLink_MetadataExtensionId_Identifier",
                table: "DbMetadataLink",
                columns: new[] { "MetadataExtensionId", "Identifier" });

            migrationBuilder.CreateIndex(
                name: "IX_MangaArtists_DbMetadataLink1Id",
                table: "MangaArtists",
                column: "DbMetadataLink1Id");

            migrationBuilder.CreateIndex(
                name: "IX_MangaAuthors_DbMetadataLinkId",
                table: "MangaAuthors",
                column: "DbMetadataLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaGenres_GenresGenre",
                table: "MangaGenres",
                column: "GenresGenre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "MangaArtists");

            migrationBuilder.DropTable(
                name: "MangaAuthors");

            migrationBuilder.DropTable(
                name: "MangaGenres");

            migrationBuilder.DropTable(
                name: "DbDownloadLink");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "DbMetadataLink");

            migrationBuilder.DropTable(
                name: "Genre");

            migrationBuilder.DropTable(
                name: "DbFile");

            migrationBuilder.DropTable(
                name: "Mangas");
        }
    }
}

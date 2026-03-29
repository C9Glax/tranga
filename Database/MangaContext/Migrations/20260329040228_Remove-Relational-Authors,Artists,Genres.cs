using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRelationalAuthorsArtistsGenres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MangaArtists");

            migrationBuilder.DropTable(
                name: "MangaAuthors");

            migrationBuilder.DropTable(
                name: "MangaGenres");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "Genre");

            migrationBuilder.AddColumn<string[]>(
                name: "Artists",
                table: "DbMetadataLink",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Authors",
                table: "DbMetadataLink",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Genres",
                table: "DbMetadataLink",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Artists",
                table: "DbMetadataLink");

            migrationBuilder.DropColumn(
                name: "Authors",
                table: "DbMetadataLink");

            migrationBuilder.DropColumn(
                name: "Genres",
                table: "DbMetadataLink");

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
    }
}

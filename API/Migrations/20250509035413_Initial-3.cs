using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class Initial3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AltTitles");

            migrationBuilder.DropTable(
                name: "Links");

            migrationBuilder.CreateTable(
                name: "Link",
                columns: table => new
                {
                    LinkId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LinkProvider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LinkUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Link", x => x.LinkId);
                    table.ForeignKey(
                        name: "FK_Link_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaAltTitle",
                columns: table => new
                {
                    AltTitleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaAltTitle", x => x.AltTitleId);
                    table.ForeignKey(
                        name: "FK_MangaAltTitle_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Link_MangaId",
                table: "Link",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaAltTitle_MangaId",
                table: "MangaAltTitle",
                column: "MangaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Link");

            migrationBuilder.DropTable(
                name: "MangaAltTitle");

            migrationBuilder.CreateTable(
                name: "AltTitles",
                columns: table => new
                {
                    AltTitleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AltTitles", x => x.AltTitleId);
                    table.ForeignKey(
                        name: "FK_AltTitles_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Links",
                columns: table => new
                {
                    LinkId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LinkProvider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LinkUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Links", x => x.LinkId);
                    table.ForeignKey(
                        name: "FK_Links_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AltTitles_MangaId",
                table: "AltTitles",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Links_MangaId",
                table: "Links",
                column: "MangaId");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations.pgsql
{
    /// <inheritdoc />
    public partial class AddMAL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetadataFetcher",
                columns: table => new
                {
                    MetadataFetcherName = table.Column<string>(type: "text", nullable: false),
                    MetadataEntry = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataFetcher", x => x.MetadataFetcherName);
                });

            migrationBuilder.CreateTable(
                name: "MetadataEntries",
                columns: table => new
                {
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: false),
                    MetadataFetcherName = table.Column<string>(type: "text", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataEntries", x => new { x.MangaId, x.MetadataFetcherName });
                    table.ForeignKey(
                        name: "FK_MetadataEntries_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetadataEntries_MetadataFetcher_MetadataFetcherName",
                        column: x => x.MetadataFetcherName,
                        principalTable: "MetadataFetcher",
                        principalColumn: "MetadataFetcherName",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetadataEntries_MetadataFetcherName",
                table: "MetadataEntries",
                column: "MetadataFetcherName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetadataEntries");

            migrationBuilder.DropTable(
                name: "MetadataFetcher");
        }
    }
}

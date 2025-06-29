using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations.pgsql
{
    /// <inheritdoc />
    public partial class MetadataEntryPrimaryKeyChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MetadataEntries",
                table: "MetadataEntries");

            migrationBuilder.DropIndex(
                name: "IX_MetadataEntries_MetadataFetcherName",
                table: "MetadataEntries");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MetadataEntries",
                table: "MetadataEntries",
                columns: new[] { "MetadataFetcherName", "Identifier" });

            migrationBuilder.CreateIndex(
                name: "IX_MetadataEntries_MangaId",
                table: "MetadataEntries",
                column: "MangaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MetadataEntries",
                table: "MetadataEntries");

            migrationBuilder.DropIndex(
                name: "IX_MetadataEntries_MangaId",
                table: "MetadataEntries");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MetadataEntries",
                table: "MetadataEntries",
                columns: new[] { "MangaId", "MetadataFetcherName" });

            migrationBuilder.CreateIndex(
                name: "IX_MetadataEntries_MetadataFetcherName",
                table: "MetadataEntries",
                column: "MetadataFetcherName");
        }
    }
}

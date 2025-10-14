using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations.Manga
{
    /// <inheritdoc />
    public partial class MetadataFetcherNotDBEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MetadataEntries_MetadataFetcher_MetadataFetcherName",
                table: "MetadataEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_MetadataEntries_MetadataFetcher_MetadataFetcherName",
                table: "MetadataEntries",
                column: "MetadataFetcherName",
                principalTable: "MetadataFetcher",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

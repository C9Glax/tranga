using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dev1603252 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mangas_LocalLibraries_LibraryLocalLibraryId",
                table: "Mangas");

            migrationBuilder.AddForeignKey(
                name: "FK_Mangas_LocalLibraries_LibraryLocalLibraryId",
                table: "Mangas",
                column: "LibraryLocalLibraryId",
                principalTable: "LocalLibraries",
                principalColumn: "LocalLibraryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mangas_LocalLibraries_LibraryLocalLibraryId",
                table: "Mangas");

            migrationBuilder.AddForeignKey(
                name: "FK_Mangas_LocalLibraries_LibraryLocalLibraryId",
                table: "Mangas",
                column: "LibraryLocalLibraryId",
                principalTable: "LocalLibraries",
                principalColumn: "LocalLibraryId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

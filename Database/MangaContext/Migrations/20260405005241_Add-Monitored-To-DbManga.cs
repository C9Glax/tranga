using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoredToDbManga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_MetadataSources_FileId",
                table: "Files");

            migrationBuilder.AddColumn<bool>(
                name: "Monitored",
                table: "Mangas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Files_MetadataSources_FileId",
                table: "Files",
                column: "FileId",
                principalTable: "MetadataSources",
                principalColumn: "CoverId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_MetadataSources_FileId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "Monitored",
                table: "Mangas");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_MetadataSources_FileId",
                table: "Files",
                column: "FileId",
                principalTable: "MetadataSources",
                principalColumn: "CoverId");
        }
    }
}

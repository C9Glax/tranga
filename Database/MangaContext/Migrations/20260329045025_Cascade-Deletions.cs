using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Files_FileId",
                table: "Chapters");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Files_FileId",
                table: "Chapters",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Files_FileId",
                table: "Chapters");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Files_FileId",
                table: "Chapters",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id");
        }
    }
}

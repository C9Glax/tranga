using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class Filesrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_DbFile_FileId",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_DbMetadataLink_DbFile_CoverId",
                table: "DbMetadataLink");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DbFile",
                table: "DbFile");

            migrationBuilder.RenameTable(
                name: "DbFile",
                newName: "Files");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Files",
                table: "Files",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Files_FileId",
                table: "Chapters",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DbMetadataLink_Files_CoverId",
                table: "DbMetadataLink",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Files_FileId",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_DbMetadataLink_Files_CoverId",
                table: "DbMetadataLink");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Files",
                table: "Files");

            migrationBuilder.RenameTable(
                name: "Files",
                newName: "DbFile");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbFile",
                table: "DbFile",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_DbFile_FileId",
                table: "Chapters",
                column: "FileId",
                principalTable: "DbFile",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DbMetadataLink_DbFile_CoverId",
                table: "DbMetadataLink",
                column: "CoverId",
                principalTable: "DbFile",
                principalColumn: "Id");
        }
    }
}

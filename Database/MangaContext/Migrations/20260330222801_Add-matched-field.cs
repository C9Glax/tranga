using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class Addmatchedfield : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_DbDownloadLink_DownloadLinkId",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_DbDownloadLink_Files_CoverId",
                table: "DbDownloadLink");

            migrationBuilder.DropForeignKey(
                name: "FK_DbDownloadLink_Mangas_MangaId",
                table: "DbDownloadLink");

            migrationBuilder.DropForeignKey(
                name: "FK_DbMetadataLink_Files_CoverId",
                table: "DbMetadataLink");

            migrationBuilder.DropForeignKey(
                name: "FK_DbMetadataLink_Mangas_MangaId",
                table: "DbMetadataLink");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DbMetadataLink",
                table: "DbMetadataLink");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DbDownloadLink",
                table: "DbDownloadLink");

            migrationBuilder.RenameTable(
                name: "DbMetadataLink",
                newName: "MetadataLinks");

            migrationBuilder.RenameTable(
                name: "DbDownloadLink",
                newName: "DownloadLinks");

            migrationBuilder.RenameIndex(
                name: "IX_DbMetadataLink_MetadataExtensionId_Identifier",
                table: "MetadataLinks",
                newName: "IX_MetadataLinks_MetadataExtensionId_Identifier");

            migrationBuilder.RenameIndex(
                name: "IX_DbMetadataLink_MangaId",
                table: "MetadataLinks",
                newName: "IX_MetadataLinks_MangaId");

            migrationBuilder.RenameIndex(
                name: "IX_DbMetadataLink_CoverId",
                table: "MetadataLinks",
                newName: "IX_MetadataLinks_CoverId");

            migrationBuilder.RenameIndex(
                name: "IX_DbDownloadLink_MangaId",
                table: "DownloadLinks",
                newName: "IX_DownloadLinks_MangaId");

            migrationBuilder.RenameIndex(
                name: "IX_DbDownloadLink_DownloadExtensionId_Identifier",
                table: "DownloadLinks",
                newName: "IX_DownloadLinks_DownloadExtensionId_Identifier");

            migrationBuilder.RenameIndex(
                name: "IX_DbDownloadLink_CoverId",
                table: "DownloadLinks",
                newName: "IX_DownloadLinks_CoverId");

            migrationBuilder.AddColumn<bool>(
                name: "Matched",
                table: "DownloadLinks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MetadataLinks",
                table: "MetadataLinks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DownloadLinks",
                table: "DownloadLinks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_DownloadLinks_DownloadLinkId",
                table: "Chapters",
                column: "DownloadLinkId",
                principalTable: "DownloadLinks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DownloadLinks_Files_CoverId",
                table: "DownloadLinks",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DownloadLinks_Mangas_MangaId",
                table: "DownloadLinks",
                column: "MangaId",
                principalTable: "Mangas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataLinks_Files_CoverId",
                table: "MetadataLinks",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataLinks_Mangas_MangaId",
                table: "MetadataLinks",
                column: "MangaId",
                principalTable: "Mangas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_DownloadLinks_DownloadLinkId",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_DownloadLinks_Files_CoverId",
                table: "DownloadLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_DownloadLinks_Mangas_MangaId",
                table: "DownloadLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataLinks_Files_CoverId",
                table: "MetadataLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataLinks_Mangas_MangaId",
                table: "MetadataLinks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MetadataLinks",
                table: "MetadataLinks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DownloadLinks",
                table: "DownloadLinks");

            migrationBuilder.DropColumn(
                name: "Matched",
                table: "DownloadLinks");

            migrationBuilder.RenameTable(
                name: "MetadataLinks",
                newName: "DbMetadataLink");

            migrationBuilder.RenameTable(
                name: "DownloadLinks",
                newName: "DbDownloadLink");

            migrationBuilder.RenameIndex(
                name: "IX_MetadataLinks_MetadataExtensionId_Identifier",
                table: "DbMetadataLink",
                newName: "IX_DbMetadataLink_MetadataExtensionId_Identifier");

            migrationBuilder.RenameIndex(
                name: "IX_MetadataLinks_MangaId",
                table: "DbMetadataLink",
                newName: "IX_DbMetadataLink_MangaId");

            migrationBuilder.RenameIndex(
                name: "IX_MetadataLinks_CoverId",
                table: "DbMetadataLink",
                newName: "IX_DbMetadataLink_CoverId");

            migrationBuilder.RenameIndex(
                name: "IX_DownloadLinks_MangaId",
                table: "DbDownloadLink",
                newName: "IX_DbDownloadLink_MangaId");

            migrationBuilder.RenameIndex(
                name: "IX_DownloadLinks_DownloadExtensionId_Identifier",
                table: "DbDownloadLink",
                newName: "IX_DbDownloadLink_DownloadExtensionId_Identifier");

            migrationBuilder.RenameIndex(
                name: "IX_DownloadLinks_CoverId",
                table: "DbDownloadLink",
                newName: "IX_DbDownloadLink_CoverId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbMetadataLink",
                table: "DbMetadataLink",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbDownloadLink",
                table: "DbDownloadLink",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_DbDownloadLink_DownloadLinkId",
                table: "Chapters",
                column: "DownloadLinkId",
                principalTable: "DbDownloadLink",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DbDownloadLink_Files_CoverId",
                table: "DbDownloadLink",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DbDownloadLink_Mangas_MangaId",
                table: "DbDownloadLink",
                column: "MangaId",
                principalTable: "Mangas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DbMetadataLink_Files_CoverId",
                table: "DbMetadataLink",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DbMetadataLink_Mangas_MangaId",
                table: "DbMetadataLink",
                column: "MangaId",
                principalTable: "Mangas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

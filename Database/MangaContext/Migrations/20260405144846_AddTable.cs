using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class AddTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DbMangaMetadataSource_Mangas_MangaId",
                table: "DbMangaMetadataSource");

            migrationBuilder.DropForeignKey(
                name: "FK_DbMangaMetadataSource_MetadataSources_MetadataSourceId",
                table: "DbMangaMetadataSource");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DbMangaMetadataSource",
                table: "DbMangaMetadataSource");

            migrationBuilder.RenameTable(
                name: "DbMangaMetadataSource",
                newName: "MangaMetadataSources");

            migrationBuilder.RenameIndex(
                name: "IX_DbMangaMetadataSource_MetadataSourceId",
                table: "MangaMetadataSources",
                newName: "IX_MangaMetadataSources_MetadataSourceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MangaMetadataSources",
                table: "MangaMetadataSources",
                columns: new[] { "MangaId", "MetadataSourceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MangaMetadataSources_Mangas_MangaId",
                table: "MangaMetadataSources",
                column: "MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MangaMetadataSources_MetadataSources_MetadataSourceId",
                table: "MangaMetadataSources",
                column: "MetadataSourceId",
                principalTable: "MetadataSources",
                principalColumn: "MetadataId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MangaMetadataSources_Mangas_MangaId",
                table: "MangaMetadataSources");

            migrationBuilder.DropForeignKey(
                name: "FK_MangaMetadataSources_MetadataSources_MetadataSourceId",
                table: "MangaMetadataSources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MangaMetadataSources",
                table: "MangaMetadataSources");

            migrationBuilder.RenameTable(
                name: "MangaMetadataSources",
                newName: "DbMangaMetadataSource");

            migrationBuilder.RenameIndex(
                name: "IX_MangaMetadataSources_MetadataSourceId",
                table: "DbMangaMetadataSource",
                newName: "IX_DbMangaMetadataSource_MetadataSourceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbMangaMetadataSource",
                table: "DbMangaMetadataSource",
                columns: new[] { "MangaId", "MetadataSourceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DbMangaMetadataSource_Mangas_MangaId",
                table: "DbMangaMetadataSource",
                column: "MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DbMangaMetadataSource_MetadataSources_MetadataSourceId",
                table: "DbMangaMetadataSource",
                column: "MetadataSourceId",
                principalTable: "MetadataSources",
                principalColumn: "MetadataId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

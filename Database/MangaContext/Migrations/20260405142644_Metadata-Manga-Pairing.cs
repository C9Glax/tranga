using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class MetadataMangaPairing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MetadataSources_Mangas_MangaId",
                table: "MetadataSources");

            migrationBuilder.DropIndex(
                name: "IX_MetadataSources_MangaId",
                table: "MetadataSources");

            migrationBuilder.DropColumn(
                name: "MangaId",
                table: "MetadataSources");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "MetadataSources");

            migrationBuilder.AddColumn<Guid>(
                name: "DbMetadataSourceMetadataId",
                table: "Mangas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DbMangaMetadataSource",
                columns: table => new
                {
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetadataSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Chosen = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbMangaMetadataSource", x => new { x.MangaId, x.MetadataSourceId });
                    table.ForeignKey(
                        name: "FK_DbMangaMetadataSource_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DbMangaMetadataSource_MetadataSources_MetadataSourceId",
                        column: x => x.MetadataSourceId,
                        principalTable: "MetadataSources",
                        principalColumn: "MetadataId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_DbMetadataSourceMetadataId",
                table: "Mangas",
                column: "DbMetadataSourceMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_DbMangaMetadataSource_MetadataSourceId",
                table: "DbMangaMetadataSource",
                column: "MetadataSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Mangas_MetadataSources_DbMetadataSourceMetadataId",
                table: "Mangas",
                column: "DbMetadataSourceMetadataId",
                principalTable: "MetadataSources",
                principalColumn: "MetadataId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mangas_MetadataSources_DbMetadataSourceMetadataId",
                table: "Mangas");

            migrationBuilder.DropTable(
                name: "DbMangaMetadataSource");

            migrationBuilder.DropIndex(
                name: "IX_Mangas_DbMetadataSourceMetadataId",
                table: "Mangas");

            migrationBuilder.DropColumn(
                name: "DbMetadataSourceMetadataId",
                table: "Mangas");

            migrationBuilder.AddColumn<Guid>(
                name: "MangaId",
                table: "MetadataSources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "MetadataSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MetadataSources_MangaId",
                table: "MetadataSources",
                column: "MangaId");

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataSources_Mangas_MangaId",
                table: "MetadataSources",
                column: "MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

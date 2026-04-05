using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class MetadataAddStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mangas_MetadataSources_DbMetadataSourceMetadataId",
                table: "Mangas");

            migrationBuilder.DropIndex(
                name: "IX_Mangas_DbMetadataSourceMetadataId",
                table: "Mangas");

            migrationBuilder.DropColumn(
                name: "DbMetadataSourceMetadataId",
                table: "Mangas");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "MetadataSources",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "MetadataSources");

            migrationBuilder.AddColumn<Guid>(
                name: "DbMetadataSourceMetadataId",
                table: "Mangas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_DbMetadataSourceMetadataId",
                table: "Mangas",
                column: "DbMetadataSourceMetadataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Mangas_MetadataSources_DbMetadataSourceMetadataId",
                table: "Mangas",
                column: "DbMetadataSourceMetadataId",
                principalTable: "MetadataSources",
                principalColumn: "MetadataId");
        }
    }
}

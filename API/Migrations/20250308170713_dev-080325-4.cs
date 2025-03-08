using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dev0803254 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MangaMangaTag_Tags_TagsTag",
                table: "MangaMangaTag");

            migrationBuilder.RenameColumn(
                name: "TagsTag",
                table: "MangaMangaTag",
                newName: "MangaTagsTag");

            migrationBuilder.RenameIndex(
                name: "IX_MangaMangaTag_TagsTag",
                table: "MangaMangaTag",
                newName: "IX_MangaMangaTag_MangaTagsTag");

            migrationBuilder.AddForeignKey(
                name: "FK_MangaMangaTag_Tags_MangaTagsTag",
                table: "MangaMangaTag",
                column: "MangaTagsTag",
                principalTable: "Tags",
                principalColumn: "Tag",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MangaMangaTag_Tags_MangaTagsTag",
                table: "MangaMangaTag");

            migrationBuilder.RenameColumn(
                name: "MangaTagsTag",
                table: "MangaMangaTag",
                newName: "TagsTag");

            migrationBuilder.RenameIndex(
                name: "IX_MangaMangaTag_MangaTagsTag",
                table: "MangaMangaTag",
                newName: "IX_MangaMangaTag_TagsTag");

            migrationBuilder.AddForeignKey(
                name: "FK_MangaMangaTag_Tags_TagsTag",
                table: "MangaMangaTag",
                column: "TagsTag",
                principalTable: "Tags",
                principalColumn: "Tag",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

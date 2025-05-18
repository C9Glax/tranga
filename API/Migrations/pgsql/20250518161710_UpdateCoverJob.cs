using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations.pgsql
{
    /// <inheritdoc />
    public partial class UpdateCoverJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UpdateCoverJob_MangaId",
                table: "Jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_UpdateCoverJob_MangaId",
                table: "Jobs",
                column: "UpdateCoverJob_MangaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_UpdateCoverJob_MangaId",
                table: "Jobs",
                column: "UpdateCoverJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_UpdateCoverJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_UpdateCoverJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "UpdateCoverJob_MangaId",
                table: "Jobs");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace API.Migrations.pgsql
{
    /// <inheritdoc />
    public partial class AltTitleOwned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MangaAltTitle",
                table: "MangaAltTitle");

            migrationBuilder.DropIndex(
                name: "IX_MangaAltTitle_MangaId",
                table: "MangaAltTitle");

            migrationBuilder.DropColumn(
                name: "AltTitleId",
                table: "MangaAltTitle");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "MangaAltTitle",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MangaAltTitle",
                table: "MangaAltTitle",
                columns: new[] { "MangaId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MangaAltTitle",
                table: "MangaAltTitle");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "MangaAltTitle");

            migrationBuilder.AddColumn<string>(
                name: "AltTitleId",
                table: "MangaAltTitle",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MangaAltTitle",
                table: "MangaAltTitle",
                column: "AltTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaAltTitle_MangaId",
                table: "MangaAltTitle",
                column: "MangaId");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.Libraries.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMangaMappingss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DirectoryMappings");

            migrationBuilder.DropTable(
                name: "Libraries");

            migrationBuilder.CreateTable(
                name: "LibraryServices",
                columns: table => new
                {
                    LibraryServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LibraryServiceType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: false),
                    ApiKey = table.Column<string>(type: "text", nullable: false),
                    TrangaLibraryId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryServices", x => x.LibraryServiceId);
                });

            migrationBuilder.CreateTable(
                name: "MangaMappings",
                columns: table => new
                {
                    LibraryServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaMappings", x => new { x.LibraryServiceId, x.MangaId });
                    table.ForeignKey(
                        name: "FK_MangaMappings_LibraryServices_LibraryServiceId",
                        column: x => x.LibraryServiceId,
                        principalTable: "LibraryServices",
                        principalColumn: "LibraryServiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MangaMappings_LibraryServiceId_SeriesId",
                table: "MangaMappings",
                columns: new[] { "LibraryServiceId", "SeriesId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MangaMappings");

            migrationBuilder.DropTable(
                name: "LibraryServices");

            migrationBuilder.CreateTable(
                name: "Libraries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKey = table.Column<string>(type: "text", nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: false),
                    LibraryType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Libraries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectoryMappings",
                columns: table => new
                {
                    MappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    LibraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePath = table.Column<string>(type: "text", nullable: false),
                    TrangaPath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryMappings", x => x.MappingId);
                    table.ForeignKey(
                        name: "FK_DirectoryMappings_Libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Libraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryMappings_LibraryId",
                table: "DirectoryMappings",
                column: "LibraryId");
        }
    }
}

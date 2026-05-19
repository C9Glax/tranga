using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.Libraries.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DirectoryMappings",
                columns: table => new
                {
                    MappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    LibraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrangaPath = table.Column<string>(type: "text", nullable: false),
                    ServicePath = table.Column<string>(type: "text", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DirectoryMappings");
        }
    }
}

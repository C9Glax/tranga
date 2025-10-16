using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations.Actions
{
    /// <inheritdoc />
    public partial class Actions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actions",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<int>(type: "integer", maxLength: 128, nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChapterId = table.Column<string>(type: "text", nullable: true),
                    MangaId = table.Column<string>(type: "text", nullable: true),
                    Filename = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    From = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    To = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    FileLibraryId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MetadataFetcher = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actions", x => x.Key);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Actions");
        }
    }
}

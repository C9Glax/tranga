using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations.library
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibraryConnectors",
                columns: table => new
                {
                    LibraryConnectorId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LibraryType = table.Column<byte>(type: "smallint", nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Auth = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryConnectors", x => x.LibraryConnectorId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibraryConnectors");
        }
    }
}

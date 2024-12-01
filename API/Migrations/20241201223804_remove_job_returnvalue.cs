using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class remove_job_returnvalue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "returnValue",
                table: "Jobs",
                newName: "SearchString");

            migrationBuilder.AddColumn<string>(
                name: "MangaConnectorName",
                table: "Jobs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MangaConnectorName",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "SearchString",
                table: "Jobs",
                newName: "returnValue");
        }
    }
}

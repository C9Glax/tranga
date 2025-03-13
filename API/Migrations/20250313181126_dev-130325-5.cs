using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dev1303255 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string[]>(
                name: "DependsOnJobsIds",
                table: "Jobs",
                type: "text[]",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string[]),
                oldType: "text[]",
                oldMaxLength: 64);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string[]>(
                name: "DependsOnJobsIds",
                table: "Jobs",
                type: "text[]",
                maxLength: 64,
                nullable: false,
                defaultValue: new string[0],
                oldClrType: typeof(string[]),
                oldType: "text[]",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}

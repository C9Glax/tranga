using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dev0703255 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_NotificationConnectors",
                table: "NotificationConnectors");

            migrationBuilder.DropColumn(
                name: "NotificationConnectorId",
                table: "NotificationConnectors");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "NotificationConnectors",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotificationConnectors",
                table: "NotificationConnectors",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_NotificationConnectors",
                table: "NotificationConnectors");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "NotificationConnectors",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "NotificationConnectorId",
                table: "NotificationConnectors",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotificationConnectors",
                table: "NotificationConnectors",
                column: "NotificationConnectorId");
        }
    }
}

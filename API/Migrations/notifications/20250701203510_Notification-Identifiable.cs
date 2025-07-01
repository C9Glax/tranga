using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations.notifications
{
    /// <inheritdoc />
    public partial class NotificationIdentifiable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NotificationId",
                table: "Notifications");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Notifications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications",
                column: "Key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Notifications");

            migrationBuilder.AddColumn<string>(
                name: "NotificationId",
                table: "Notifications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications",
                column: "NotificationId");
        }
    }
}

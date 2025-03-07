using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dev0703253 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppToken",
                table: "NotificationConnectors");

            migrationBuilder.DropColumn(
                name: "Auth",
                table: "NotificationConnectors");

            migrationBuilder.DropColumn(
                name: "Endpoint",
                table: "NotificationConnectors");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "NotificationConnectors");

            migrationBuilder.DropColumn(
                name: "NotificationConnectorType",
                table: "NotificationConnectors");

            migrationBuilder.DropColumn(
                name: "Ntfy_Endpoint",
                table: "NotificationConnectors");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "NotificationConnectors");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "NotificationConnectors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "Headers",
                table: "NotificationConnectors",
                type: "hstore",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "NotificationConnectors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "NotificationConnectors",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Body",
                table: "NotificationConnectors");

            migrationBuilder.DropColumn(
                name: "Headers",
                table: "NotificationConnectors");

            migrationBuilder.DropColumn(
                name: "HttpMethod",
                table: "NotificationConnectors");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "NotificationConnectors");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<string>(
                name: "AppToken",
                table: "NotificationConnectors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Auth",
                table: "NotificationConnectors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Endpoint",
                table: "NotificationConnectors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "NotificationConnectors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "NotificationConnectorType",
                table: "NotificationConnectors",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "Ntfy_Endpoint",
                table: "NotificationConnectors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "NotificationConnectors",
                type: "text",
                nullable: true);
        }
    }
}

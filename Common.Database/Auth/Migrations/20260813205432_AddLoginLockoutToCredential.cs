using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Database.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginLockoutToCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Credentials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedUntil",
                table: "Credentials",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "Credentials");
        }
    }
}

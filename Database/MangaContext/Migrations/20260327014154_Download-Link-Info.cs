using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.MangaContext.Migrations
{
    /// <inheritdoc />
    public partial class DownloadLinkInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CoverId",
                table: "DbDownloadLink",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "DbDownloadLink",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "DbDownloadLink",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DbDownloadLink_CoverId",
                table: "DbDownloadLink",
                column: "CoverId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DbDownloadLink_Files_CoverId",
                table: "DbDownloadLink",
                column: "CoverId",
                principalTable: "Files",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DbDownloadLink_Files_CoverId",
                table: "DbDownloadLink");

            migrationBuilder.DropIndex(
                name: "IX_DbDownloadLink_CoverId",
                table: "DbDownloadLink");

            migrationBuilder.DropColumn(
                name: "CoverId",
                table: "DbDownloadLink");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "DbDownloadLink");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "DbDownloadLink");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.Tasks.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskType = table.Column<byte>(type: "smallint", nullable: false),
                    LastRun = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HasRun = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.TaskId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tasks");
        }
    }
}

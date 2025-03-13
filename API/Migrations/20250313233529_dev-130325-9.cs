using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dev1303259 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Jobs_JobId1",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Jobs_ParentJobId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_JobId1",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "JobId1",
                table: "Jobs");

            migrationBuilder.CreateTable(
                name: "JobJob",
                columns: table => new
                {
                    DependsOnJobsJobId = table.Column<string>(type: "character varying(64)", nullable: false),
                    JobId = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobJob", x => new { x.DependsOnJobsJobId, x.JobId });
                    table.ForeignKey(
                        name: "FK_JobJob_Jobs_DependsOnJobsJobId",
                        column: x => x.DependsOnJobsJobId,
                        principalTable: "Jobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobJob_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobJob_JobId",
                table: "JobJob",
                column: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Jobs_ParentJobId",
                table: "Jobs",
                column: "ParentJobId",
                principalTable: "Jobs",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Jobs_ParentJobId",
                table: "Jobs");

            migrationBuilder.DropTable(
                name: "JobJob");

            migrationBuilder.AddColumn<string>(
                name: "JobId1",
                table: "Jobs",
                type: "character varying(64)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobId1",
                table: "Jobs",
                column: "JobId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Jobs_JobId1",
                table: "Jobs",
                column: "JobId1",
                principalTable: "Jobs",
                principalColumn: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Jobs_ParentJobId",
                table: "Jobs",
                column: "ParentJobId",
                principalTable: "Jobs",
                principalColumn: "JobId");
        }
    }
}

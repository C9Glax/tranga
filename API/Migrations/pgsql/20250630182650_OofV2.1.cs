using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations.pgsql
{
    /// <inheritdoc />
    public partial class OofV21 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorToManga_Authors_AuthorIds",
                table: "AuthorToManga");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorToManga_Mangas_MangaIds",
                table: "AuthorToManga");

            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Mangas_ParentMangaId",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_JobJob_Jobs_DependsOnJobsJobId",
                table: "JobJob");

            migrationBuilder.DropForeignKey(
                name: "FK_JobJob_Jobs_JobId",
                table: "JobJob");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Chapters_ChapterId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Jobs_ParentJobId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_LocalLibraries_ToLibraryId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_MoveMangaLibraryJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_RetrieveChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_UpdateChaptersDownloadedJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_UpdateCoverJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Link_Mangas_MangaId",
                table: "Link");

            migrationBuilder.DropForeignKey(
                name: "FK_Mangas_LocalLibraries_LibraryId",
                table: "Mangas");

            migrationBuilder.DropForeignKey(
                name: "FK_Mangas_MangaConnectors_MangaConnectorName",
                table: "Mangas");

            migrationBuilder.DropForeignKey(
                name: "FK_MangaTagToManga_Mangas_MangaIds",
                table: "MangaTagToManga");

            migrationBuilder.DropTable(
                name: "MangaAltTitle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Mangas",
                table: "Mangas");

            migrationBuilder.DropIndex(
                name: "IX_Mangas_MangaConnectorName",
                table: "Mangas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalLibraries",
                table: "LocalLibraries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Link",
                table: "Link");

            migrationBuilder.DropIndex(
                name: "IX_Link_MangaId",
                table: "Link");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Jobs",
                table: "Jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JobJob",
                table: "JobJob");

            migrationBuilder.DropIndex(
                name: "IX_JobJob_JobId",
                table: "JobJob");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Chapters",
                table: "Chapters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Authors",
                table: "Authors");

            migrationBuilder.DropColumn(
                name: "MangaId",
                table: "Mangas");

            migrationBuilder.DropColumn(
                name: "IdOnConnectorSite",
                table: "Mangas");

            migrationBuilder.DropColumn(
                name: "MangaConnectorName",
                table: "Mangas");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "Mangas");

            migrationBuilder.DropColumn(
                name: "LocalLibraryId",
                table: "LocalLibraries");

            migrationBuilder.DropColumn(
                name: "LinkId",
                table: "Link");

            migrationBuilder.DropColumn(
                name: "MangaId",
                table: "Link");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DependsOnJobsJobId",
                table: "JobJob");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "JobJob");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "IdOnConnectorSite",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Authors");

            migrationBuilder.AlterColumn<string>(
                name: "MangaIds",
                table: "MangaTagToManga",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Mangas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "LocalLibraries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Link",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MangaKey",
                table: "Link",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Jobs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DependsOnJobsKey",
                table: "JobJob",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JobKey",
                table: "JobJob",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Chapters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "MangaIds",
                table: "AuthorToManga",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorIds",
                table: "AuthorToManga",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Authors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Mangas",
                table: "Mangas",
                column: "Key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalLibraries",
                table: "LocalLibraries",
                column: "Key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Link",
                table: "Link",
                column: "Key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Jobs",
                table: "Jobs",
                column: "Key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JobJob",
                table: "JobJob",
                columns: new[] { "DependsOnJobsKey", "JobKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Chapters",
                table: "Chapters",
                column: "Key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Authors",
                table: "Authors",
                column: "Key");

            migrationBuilder.CreateTable(
                name: "AltTitle",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MangaKey = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AltTitle", x => x.Key);
                    table.ForeignKey(
                        name: "FK_AltTitle_Mangas_MangaKey",
                        column: x => x.MangaKey,
                        principalTable: "Mangas",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaConnectorToChapter",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    ObjId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MangaConnectorName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IdOnConnectorSite = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaConnectorToChapter", x => x.Key);
                    table.ForeignKey(
                        name: "FK_MangaConnectorToChapter_Chapters_ObjId",
                        column: x => x.ObjId,
                        principalTable: "Chapters",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_MangaConnectorToChapter_MangaConnectors_MangaConnectorName",
                        column: x => x.MangaConnectorName,
                        principalTable: "MangaConnectors",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaConnectorToManga",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    ObjId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MangaConnectorName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IdOnConnectorSite = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaConnectorToManga", x => x.Key);
                    table.ForeignKey(
                        name: "FK_MangaConnectorToManga_MangaConnectors_MangaConnectorName",
                        column: x => x.MangaConnectorName,
                        principalTable: "MangaConnectors",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaConnectorToManga_Mangas_ObjId",
                        column: x => x.ObjId,
                        principalTable: "Mangas",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Link_MangaKey",
                table: "Link",
                column: "MangaKey");

            migrationBuilder.CreateIndex(
                name: "IX_JobJob_JobKey",
                table: "JobJob",
                column: "JobKey");

            migrationBuilder.CreateIndex(
                name: "IX_AltTitle_MangaKey",
                table: "AltTitle",
                column: "MangaKey");

            migrationBuilder.CreateIndex(
                name: "IX_MangaConnectorToChapter_MangaConnectorName",
                table: "MangaConnectorToChapter",
                column: "MangaConnectorName");

            migrationBuilder.CreateIndex(
                name: "IX_MangaConnectorToChapter_ObjId",
                table: "MangaConnectorToChapter",
                column: "ObjId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaConnectorToManga_MangaConnectorName",
                table: "MangaConnectorToManga",
                column: "MangaConnectorName");

            migrationBuilder.CreateIndex(
                name: "IX_MangaConnectorToManga_ObjId",
                table: "MangaConnectorToManga",
                column: "ObjId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorToManga_Authors_AuthorIds",
                table: "AuthorToManga",
                column: "AuthorIds",
                principalTable: "Authors",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorToManga_Mangas_MangaIds",
                table: "AuthorToManga",
                column: "MangaIds",
                principalTable: "Mangas",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Mangas_ParentMangaId",
                table: "Chapters",
                column: "ParentMangaId",
                principalTable: "Mangas",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobJob_Jobs_DependsOnJobsKey",
                table: "JobJob",
                column: "DependsOnJobsKey",
                principalTable: "Jobs",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobJob_Jobs_JobKey",
                table: "JobJob",
                column: "JobKey",
                principalTable: "Jobs",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Chapters_ChapterId",
                table: "Jobs",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Jobs_ParentJobId",
                table: "Jobs",
                column: "ParentJobId",
                principalTable: "Jobs",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_LocalLibraries_ToLibraryId",
                table: "Jobs",
                column: "ToLibraryId",
                principalTable: "LocalLibraries",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs",
                column: "DownloadAvailableChaptersJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_MangaId",
                table: "Jobs",
                column: "MangaId",
                principalTable: "Mangas",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_MoveMangaLibraryJob_MangaId",
                table: "Jobs",
                column: "MoveMangaLibraryJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_RetrieveChaptersJob_MangaId",
                table: "Jobs",
                column: "RetrieveChaptersJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_UpdateChaptersDownloadedJob_MangaId",
                table: "Jobs",
                column: "UpdateChaptersDownloadedJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_UpdateCoverJob_MangaId",
                table: "Jobs",
                column: "UpdateCoverJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Link_Mangas_MangaKey",
                table: "Link",
                column: "MangaKey",
                principalTable: "Mangas",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mangas_LocalLibraries_LibraryId",
                table: "Mangas",
                column: "LibraryId",
                principalTable: "LocalLibraries",
                principalColumn: "Key",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MangaTagToManga_Mangas_MangaIds",
                table: "MangaTagToManga",
                column: "MangaIds",
                principalTable: "Mangas",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorToManga_Authors_AuthorIds",
                table: "AuthorToManga");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorToManga_Mangas_MangaIds",
                table: "AuthorToManga");

            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Mangas_ParentMangaId",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_JobJob_Jobs_DependsOnJobsKey",
                table: "JobJob");

            migrationBuilder.DropForeignKey(
                name: "FK_JobJob_Jobs_JobKey",
                table: "JobJob");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Chapters_ChapterId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Jobs_ParentJobId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_LocalLibraries_ToLibraryId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_MoveMangaLibraryJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_RetrieveChaptersJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_UpdateChaptersDownloadedJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Mangas_UpdateCoverJob_MangaId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Link_Mangas_MangaKey",
                table: "Link");

            migrationBuilder.DropForeignKey(
                name: "FK_Mangas_LocalLibraries_LibraryId",
                table: "Mangas");

            migrationBuilder.DropForeignKey(
                name: "FK_MangaTagToManga_Mangas_MangaIds",
                table: "MangaTagToManga");

            migrationBuilder.DropTable(
                name: "AltTitle");

            migrationBuilder.DropTable(
                name: "MangaConnectorToChapter");

            migrationBuilder.DropTable(
                name: "MangaConnectorToManga");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Mangas",
                table: "Mangas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalLibraries",
                table: "LocalLibraries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Link",
                table: "Link");

            migrationBuilder.DropIndex(
                name: "IX_Link_MangaKey",
                table: "Link");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Jobs",
                table: "Jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JobJob",
                table: "JobJob");

            migrationBuilder.DropIndex(
                name: "IX_JobJob_JobKey",
                table: "JobJob");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Chapters",
                table: "Chapters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Authors",
                table: "Authors");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Mangas");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "LocalLibraries");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Link");

            migrationBuilder.DropColumn(
                name: "MangaKey",
                table: "Link");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DependsOnJobsKey",
                table: "JobJob");

            migrationBuilder.DropColumn(
                name: "JobKey",
                table: "JobJob");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Authors");

            migrationBuilder.AlterColumn<string>(
                name: "MangaIds",
                table: "MangaTagToManga",
                type: "character varying(64)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "MangaId",
                table: "Mangas",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdOnConnectorSite",
                table: "Mangas",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MangaConnectorName",
                table: "Mangas",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "Mangas",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LocalLibraryId",
                table: "LocalLibraries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LinkId",
                table: "Link",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MangaId",
                table: "Link",
                type: "character varying(64)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JobId",
                table: "Jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DependsOnJobsJobId",
                table: "JobJob",
                type: "character varying(64)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JobId",
                table: "JobJob",
                type: "character varying(64)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChapterId",
                table: "Chapters",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdOnConnectorSite",
                table: "Chapters",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Chapters",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "MangaIds",
                table: "AuthorToManga",
                type: "character varying(64)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorIds",
                table: "AuthorToManga",
                type: "character varying(64)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "AuthorId",
                table: "Authors",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Mangas",
                table: "Mangas",
                column: "MangaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalLibraries",
                table: "LocalLibraries",
                column: "LocalLibraryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Link",
                table: "Link",
                column: "LinkId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Jobs",
                table: "Jobs",
                column: "JobId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JobJob",
                table: "JobJob",
                columns: new[] { "DependsOnJobsJobId", "JobId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Chapters",
                table: "Chapters",
                column: "ChapterId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Authors",
                table: "Authors",
                column: "AuthorId");

            migrationBuilder.CreateTable(
                name: "MangaAltTitle",
                columns: table => new
                {
                    AltTitleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaAltTitle", x => x.AltTitleId);
                    table.ForeignKey(
                        name: "FK_MangaAltTitle_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_MangaConnectorName",
                table: "Mangas",
                column: "MangaConnectorName");

            migrationBuilder.CreateIndex(
                name: "IX_Link_MangaId",
                table: "Link",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_JobJob_JobId",
                table: "JobJob",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaAltTitle_MangaId",
                table: "MangaAltTitle",
                column: "MangaId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorToManga_Authors_AuthorIds",
                table: "AuthorToManga",
                column: "AuthorIds",
                principalTable: "Authors",
                principalColumn: "AuthorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorToManga_Mangas_MangaIds",
                table: "AuthorToManga",
                column: "MangaIds",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Mangas_ParentMangaId",
                table: "Chapters",
                column: "ParentMangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobJob_Jobs_DependsOnJobsJobId",
                table: "JobJob",
                column: "DependsOnJobsJobId",
                principalTable: "Jobs",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobJob_Jobs_JobId",
                table: "JobJob",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Chapters_ChapterId",
                table: "Jobs",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "ChapterId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Jobs_ParentJobId",
                table: "Jobs",
                column: "ParentJobId",
                principalTable: "Jobs",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_LocalLibraries_ToLibraryId",
                table: "Jobs",
                column: "ToLibraryId",
                principalTable: "LocalLibraries",
                principalColumn: "LocalLibraryId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs",
                column: "DownloadAvailableChaptersJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_MangaId",
                table: "Jobs",
                column: "MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_MoveMangaLibraryJob_MangaId",
                table: "Jobs",
                column: "MoveMangaLibraryJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_RetrieveChaptersJob_MangaId",
                table: "Jobs",
                column: "RetrieveChaptersJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_UpdateChaptersDownloadedJob_MangaId",
                table: "Jobs",
                column: "UpdateChaptersDownloadedJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Mangas_UpdateCoverJob_MangaId",
                table: "Jobs",
                column: "UpdateCoverJob_MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Link_Mangas_MangaId",
                table: "Link",
                column: "MangaId",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mangas_LocalLibraries_LibraryId",
                table: "Mangas",
                column: "LibraryId",
                principalTable: "LocalLibraries",
                principalColumn: "LocalLibraryId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Mangas_MangaConnectors_MangaConnectorName",
                table: "Mangas",
                column: "MangaConnectorName",
                principalTable: "MangaConnectors",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MangaTagToManga_Mangas_MangaIds",
                table: "MangaTagToManga",
                column: "MangaIds",
                principalTable: "Mangas",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

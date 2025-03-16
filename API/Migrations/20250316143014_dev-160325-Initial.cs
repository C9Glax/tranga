using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dev160325Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    AuthorId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.AuthorId);
                });

            migrationBuilder.CreateTable(
                name: "LibraryConnectors",
                columns: table => new
                {
                    LibraryConnectorId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LibraryType = table.Column<byte>(type: "smallint", nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Auth = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryConnectors", x => x.LibraryConnectorId);
                });

            migrationBuilder.CreateTable(
                name: "LocalLibraries",
                columns: table => new
                {
                    LocalLibraryId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BasePath = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LibraryName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalLibraries", x => x.LocalLibraryId);
                });

            migrationBuilder.CreateTable(
                name: "MangaConnectors",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SupportedLanguages = table.Column<string[]>(type: "text[]", maxLength: 8, nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    BaseUris = table.Column<string[]>(type: "text[]", maxLength: 256, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaConnectors", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "NotificationConnectors",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Headers = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    HttpMethod = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Body = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationConnectors", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Urgency = table.Column<byte>(type: "smallint", nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Tag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Tag);
                });

            migrationBuilder.CreateTable(
                name: "Mangas",
                columns: table => new
                {
                    MangaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IdOnConnectorSite = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CoverUrl = table.Column<string>(type: "text", nullable: false),
                    CoverFileNameInCache = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<long>(type: "bigint", nullable: false),
                    OriginalLanguage = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ReleaseStatus = table.Column<byte>(type: "smallint", nullable: false),
                    DirectoryName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LibraryLocalLibraryId = table.Column<string>(type: "character varying(64)", nullable: true),
                    IgnoreChapterBefore = table.Column<float>(type: "real", nullable: false),
                    MangaConnectorId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mangas", x => x.MangaId);
                    table.ForeignKey(
                        name: "FK_Mangas_LocalLibraries_LibraryLocalLibraryId",
                        column: x => x.LibraryLocalLibraryId,
                        principalTable: "LocalLibraries",
                        principalColumn: "LocalLibraryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Mangas_MangaConnectors_MangaConnectorId",
                        column: x => x.MangaConnectorId,
                        principalTable: "MangaConnectors",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AltTitles",
                columns: table => new
                {
                    AltTitleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AltTitles", x => x.AltTitleId);
                    table.ForeignKey(
                        name: "FK_AltTitles_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthorManga",
                columns: table => new
                {
                    AuthorsAuthorId = table.Column<string>(type: "character varying(64)", nullable: false),
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorManga", x => new { x.AuthorsAuthorId, x.MangaId });
                    table.ForeignKey(
                        name: "FK_AuthorManga_Authors_AuthorsAuthorId",
                        column: x => x.AuthorsAuthorId,
                        principalTable: "Authors",
                        principalColumn: "AuthorId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorManga_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    ChapterId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VolumeNumber = table.Column<int>(type: "integer", nullable: true),
                    ChapterNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Downloaded = table.Column<bool>(type: "boolean", nullable: false),
                    ParentMangaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.ChapterId);
                    table.ForeignKey(
                        name: "FK_Chapters_Mangas_ParentMangaId",
                        column: x => x.ParentMangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Links",
                columns: table => new
                {
                    LinkId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LinkProvider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LinkUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Links", x => x.LinkId);
                    table.ForeignKey(
                        name: "FK_Links_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaMangaTag",
                columns: table => new
                {
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: false),
                    MangaTagsTag = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaMangaTag", x => new { x.MangaId, x.MangaTagsTag });
                    table.ForeignKey(
                        name: "FK_MangaMangaTag_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaMangaTag_Tags_MangaTagsTag",
                        column: x => x.MangaTagsTag,
                        principalTable: "Tags",
                        principalColumn: "Tag",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    JobId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParentJobId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DependsOnJobsIds = table.Column<string[]>(type: "text[]", maxLength: 64, nullable: true),
                    JobType = table.Column<byte>(type: "smallint", nullable: false),
                    RecurrenceMs = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LastExecution = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    state = table.Column<byte>(type: "smallint", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    DownloadAvailableChaptersJob_MangaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MangaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ChapterId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FromLocation = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ToLocation = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RetrieveChaptersJob_MangaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UpdateFilesDownloadedJob_MangaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UpdateMetadataJob_MangaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.JobId);
                    table.ForeignKey(
                        name: "FK_Jobs_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_Jobs_ParentJobId",
                        column: x => x.ParentJobId,
                        principalTable: "Jobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_Mangas_DownloadAvailableChaptersJob_MangaId",
                        column: x => x.DownloadAvailableChaptersJob_MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_Mangas_RetrieveChaptersJob_MangaId",
                        column: x => x.RetrieveChaptersJob_MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_Mangas_UpdateFilesDownloadedJob_MangaId",
                        column: x => x.UpdateFilesDownloadedJob_MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_Mangas_UpdateMetadataJob_MangaId",
                        column: x => x.UpdateMetadataJob_MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_AltTitles_MangaId",
                table: "AltTitles",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorManga_MangaId",
                table: "AuthorManga",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_ParentMangaId",
                table: "Chapters",
                column: "ParentMangaId");

            migrationBuilder.CreateIndex(
                name: "IX_JobJob_JobId",
                table: "JobJob",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ChapterId",
                table: "Jobs",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_DownloadAvailableChaptersJob_MangaId",
                table: "Jobs",
                column: "DownloadAvailableChaptersJob_MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_MangaId",
                table: "Jobs",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ParentJobId",
                table: "Jobs",
                column: "ParentJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_RetrieveChaptersJob_MangaId",
                table: "Jobs",
                column: "RetrieveChaptersJob_MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_UpdateFilesDownloadedJob_MangaId",
                table: "Jobs",
                column: "UpdateFilesDownloadedJob_MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_UpdateMetadataJob_MangaId",
                table: "Jobs",
                column: "UpdateMetadataJob_MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Links_MangaId",
                table: "Links",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaMangaTag_MangaTagsTag",
                table: "MangaMangaTag",
                column: "MangaTagsTag");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_LibraryLocalLibraryId",
                table: "Mangas",
                column: "LibraryLocalLibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_MangaConnectorId",
                table: "Mangas",
                column: "MangaConnectorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AltTitles");

            migrationBuilder.DropTable(
                name: "AuthorManga");

            migrationBuilder.DropTable(
                name: "JobJob");

            migrationBuilder.DropTable(
                name: "LibraryConnectors");

            migrationBuilder.DropTable(
                name: "Links");

            migrationBuilder.DropTable(
                name: "MangaMangaTag");

            migrationBuilder.DropTable(
                name: "NotificationConnectors");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Mangas");

            migrationBuilder.DropTable(
                name: "LocalLibraries");

            migrationBuilder.DropTable(
                name: "MangaConnectors");
        }
    }
}

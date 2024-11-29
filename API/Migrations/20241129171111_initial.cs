using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    AuthorId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AuthorName = table.Column<string>(type: "text", nullable: false)
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
                    BaseUrl = table.Column<string>(type: "text", nullable: false),
                    Auth = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryConnectors", x => x.LibraryConnectorId);
                });

            migrationBuilder.CreateTable(
                name: "MangaConnectors",
                columns: table => new
                {
                    MangaConnectorId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SupportedLanguages = table.Column<string[]>(type: "text[]", nullable: false),
                    BaseUris = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaConnectors", x => x.MangaConnectorId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationConnectors",
                columns: table => new
                {
                    NotificationConnectorId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NotificationConnectorType = table.Column<byte>(type: "smallint", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: true),
                    AppToken = table.Column<string>(type: "text", nullable: true),
                    Id = table.Column<string>(type: "text", nullable: true),
                    Ntfy_Endpoint = table.Column<string>(type: "text", nullable: true),
                    Auth = table.Column<string>(type: "text", nullable: true),
                    Topic = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationConnectors", x => x.NotificationConnectorId);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Tag = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Tag);
                });

            migrationBuilder.CreateTable(
                name: "AltTitles",
                columns: table => new
                {
                    AltTitleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: false),
                    AltTitleIds = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AltTitles", x => x.AltTitleId);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    ChapterId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VolumeNumber = table.Column<float>(type: "real", nullable: true),
                    ChapterNumber = table.Column<float>(type: "real", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    ArchiveFileName = table.Column<string>(type: "text", nullable: false),
                    Downloaded = table.Column<bool>(type: "boolean", nullable: false),
                    ParentMangaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChapterIds = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.ChapterId);
                });

            migrationBuilder.CreateTable(
                name: "Manga",
                columns: table => new
                {
                    MangaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConnectorId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CoverUrl = table.Column<string>(type: "text", nullable: false),
                    CoverFileNameInCache = table.Column<string>(type: "text", nullable: true),
                    year = table.Column<long>(type: "bigint", nullable: false),
                    OriginalLanguage = table.Column<string>(type: "text", nullable: true),
                    ReleaseStatus = table.Column<byte>(type: "smallint", nullable: false),
                    FolderName = table.Column<string>(type: "text", nullable: false),
                    IgnoreChapterBefore = table.Column<float>(type: "real", nullable: false),
                    LatestChapterDownloadedId = table.Column<string>(type: "character varying(64)", nullable: true),
                    LatestChapterAvailableId = table.Column<string>(type: "character varying(64)", nullable: true),
                    MangaConnectorId = table.Column<string>(type: "character varying(64)", nullable: false),
                    AuthorIds = table.Column<string[]>(type: "text[]", nullable: false),
                    TagIds = table.Column<string[]>(type: "text[]", nullable: false),
                    LinkIds = table.Column<string[]>(type: "text[]", nullable: false),
                    AltTitleIds = table.Column<string[]>(type: "text[]", nullable: false),
                    MangaIds = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manga", x => x.MangaId);
                    table.ForeignKey(
                        name: "FK_Manga_Chapters_LatestChapterAvailableId",
                        column: x => x.LatestChapterAvailableId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId");
                    table.ForeignKey(
                        name: "FK_Manga_Chapters_LatestChapterDownloadedId",
                        column: x => x.LatestChapterDownloadedId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId");
                    table.ForeignKey(
                        name: "FK_Manga_MangaConnectors_MangaConnectorId",
                        column: x => x.MangaConnectorId,
                        principalTable: "MangaConnectors",
                        principalColumn: "MangaConnectorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    JobId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParentJobId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DependsOnJobIds = table.Column<string[]>(type: "text[]", maxLength: 64, nullable: true),
                    JobType = table.Column<byte>(type: "smallint", nullable: false),
                    RecurrenceMs = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LastExecution = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextExecution = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    returnValue = table.Column<string>(type: "text", nullable: true),
                    JobId1 = table.Column<string>(type: "character varying(64)", nullable: true),
                    ImagesLocation = table.Column<string>(type: "text", nullable: true),
                    CreateArchiveJob_ChapterId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreateComicInfoXmlJob_ChapterId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MangaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ChapterId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FromLocation = table.Column<string>(type: "text", nullable: true),
                    ToLocation = table.Column<string>(type: "text", nullable: true),
                    Path = table.Column<string>(type: "text", nullable: true),
                    Bw = table.Column<bool>(type: "boolean", nullable: true),
                    Compression = table.Column<int>(type: "integer", nullable: true),
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
                        name: "FK_Jobs_Chapters_CreateArchiveJob_ChapterId",
                        column: x => x.CreateArchiveJob_ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_Chapters_CreateComicInfoXmlJob_ChapterId",
                        column: x => x.CreateComicInfoXmlJob_ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_Jobs_JobId1",
                        column: x => x.JobId1,
                        principalTable: "Jobs",
                        principalColumn: "JobId");
                    table.ForeignKey(
                        name: "FK_Jobs_Manga_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Manga",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_Manga_UpdateMetadataJob_MangaId",
                        column: x => x.UpdateMetadataJob_MangaId,
                        principalTable: "Manga",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Link",
                columns: table => new
                {
                    LinkId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LinkProvider = table.Column<string>(type: "text", nullable: false),
                    LinkUrl = table.Column<string>(type: "text", nullable: false),
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: false),
                    LinkIds = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Link", x => x.LinkId);
                    table.ForeignKey(
                        name: "FK_Link_Manga_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Manga",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaAuthor",
                columns: table => new
                {
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: false),
                    AuthorId = table.Column<string>(type: "character varying(64)", nullable: false),
                    AuthorIds = table.Column<string>(type: "text", nullable: true),
                    MangaIds = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaAuthor", x => new { x.MangaId, x.AuthorId });
                    table.ForeignKey(
                        name: "FK_MangaAuthor_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "AuthorId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaAuthor_Manga_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Manga",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaTag",
                columns: table => new
                {
                    MangaId = table.Column<string>(type: "character varying(64)", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: false),
                    MangaIds = table.Column<string>(type: "text", nullable: false),
                    TagIds = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaTag", x => new { x.MangaId, x.Tag });
                    table.ForeignKey(
                        name: "FK_MangaTag_Manga_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Manga",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaTag_Tags_MangaIds",
                        column: x => x.MangaIds,
                        principalTable: "Tags",
                        principalColumn: "Tag",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaTag_Tags_Tag",
                        column: x => x.Tag,
                        principalTable: "Tags",
                        principalColumn: "Tag",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AltTitles_MangaId",
                table: "AltTitles",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_ParentMangaId",
                table: "Chapters",
                column: "ParentMangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ChapterId",
                table: "Jobs",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CreateArchiveJob_ChapterId",
                table: "Jobs",
                column: "CreateArchiveJob_ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CreateComicInfoXmlJob_ChapterId",
                table: "Jobs",
                column: "CreateComicInfoXmlJob_ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobId1",
                table: "Jobs",
                column: "JobId1");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_MangaId",
                table: "Jobs",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_UpdateMetadataJob_MangaId",
                table: "Jobs",
                column: "UpdateMetadataJob_MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Link_MangaId",
                table: "Link",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Manga_LatestChapterAvailableId",
                table: "Manga",
                column: "LatestChapterAvailableId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Manga_LatestChapterDownloadedId",
                table: "Manga",
                column: "LatestChapterDownloadedId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Manga_MangaConnectorId",
                table: "Manga",
                column: "MangaConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaAuthor_AuthorId",
                table: "MangaAuthor",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaTag_MangaIds",
                table: "MangaTag",
                column: "MangaIds");

            migrationBuilder.CreateIndex(
                name: "IX_MangaTag_Tag",
                table: "MangaTag",
                column: "Tag");

            migrationBuilder.AddForeignKey(
                name: "FK_AltTitles_Manga_MangaId",
                table: "AltTitles",
                column: "MangaId",
                principalTable: "Manga",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Manga_ParentMangaId",
                table: "Chapters",
                column: "ParentMangaId",
                principalTable: "Manga",
                principalColumn: "MangaId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Manga_ParentMangaId",
                table: "Chapters");

            migrationBuilder.DropTable(
                name: "AltTitles");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "LibraryConnectors");

            migrationBuilder.DropTable(
                name: "Link");

            migrationBuilder.DropTable(
                name: "MangaAuthor");

            migrationBuilder.DropTable(
                name: "MangaTag");

            migrationBuilder.DropTable(
                name: "NotificationConnectors");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Manga");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "MangaConnectors");
        }
    }
}

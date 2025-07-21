using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations.Manga
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "FileLibraries",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    BasePath = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LibraryName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileLibraries", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "MangaConnector",
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
                    table.PrimaryKey("PK_MangaConnector", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "MetadataFetcher",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    MetadataEntry = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataFetcher", x => x.Name);
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
                    Key = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CoverUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ReleaseStatus = table.Column<byte>(type: "smallint", nullable: false),
                    LibraryId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IgnoreChaptersBefore = table.Column<float>(type: "real", nullable: false),
                    DirectoryName = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    CoverFileNameInCache = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Year = table.Column<long>(type: "bigint", nullable: true),
                    OriginalLanguage = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mangas", x => x.Key);
                    table.ForeignKey(
                        name: "FK_Mangas_FileLibraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "FileLibraries",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.SetNull);
                });

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
                name: "AuthorToManga",
                columns: table => new
                {
                    AuthorIds = table.Column<string>(type: "text", nullable: false),
                    MangaIds = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorToManga", x => new { x.AuthorIds, x.MangaIds });
                    table.ForeignKey(
                        name: "FK_AuthorToManga_Authors_AuthorIds",
                        column: x => x.AuthorIds,
                        principalTable: "Authors",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorToManga_Mangas_MangaIds",
                        column: x => x.MangaIds,
                        principalTable: "Mangas",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    ParentMangaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VolumeNumber = table.Column<int>(type: "integer", nullable: true),
                    ChapterNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Downloaded = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Key);
                    table.ForeignKey(
                        name: "FK_Chapters_Mangas_ParentMangaId",
                        column: x => x.ParentMangaId,
                        principalTable: "Mangas",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Link",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    LinkProvider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LinkUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    MangaKey = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Link", x => x.Key);
                    table.ForeignKey(
                        name: "FK_Link_Mangas_MangaKey",
                        column: x => x.MangaKey,
                        principalTable: "Mangas",
                        principalColumn: "Key",
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
                    WebsiteUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    UseForDownload = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaConnectorToManga", x => x.Key);
                    table.ForeignKey(
                        name: "FK_MangaConnectorToManga_Mangas_ObjId",
                        column: x => x.ObjId,
                        principalTable: "Mangas",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaTagToManga",
                columns: table => new
                {
                    MangaTagIds = table.Column<string>(type: "character varying(64)", nullable: false),
                    MangaIds = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaTagToManga", x => new { x.MangaTagIds, x.MangaIds });
                    table.ForeignKey(
                        name: "FK_MangaTagToManga_Mangas_MangaIds",
                        column: x => x.MangaIds,
                        principalTable: "Mangas",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaTagToManga_Tags_MangaTagIds",
                        column: x => x.MangaTagIds,
                        principalTable: "Tags",
                        principalColumn: "Tag",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetadataEntries",
                columns: table => new
                {
                    MetadataFetcherName = table.Column<string>(type: "text", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    MangaId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataEntries", x => new { x.MetadataFetcherName, x.Identifier });
                    table.ForeignKey(
                        name: "FK_MetadataEntries_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetadataEntries_MetadataFetcher_MetadataFetcherName",
                        column: x => x.MetadataFetcherName,
                        principalTable: "MetadataFetcher",
                        principalColumn: "Name",
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
                    WebsiteUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    UseForDownload = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaConnectorToChapter", x => x.Key);
                    table.ForeignKey(
                        name: "FK_MangaConnectorToChapter_Chapters_ObjId",
                        column: x => x.ObjId,
                        principalTable: "Chapters",
                        principalColumn: "Key");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AltTitle_MangaKey",
                table: "AltTitle",
                column: "MangaKey");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorToManga_MangaIds",
                table: "AuthorToManga",
                column: "MangaIds");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_ParentMangaId",
                table: "Chapters",
                column: "ParentMangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Link_MangaKey",
                table: "Link",
                column: "MangaKey");

            migrationBuilder.CreateIndex(
                name: "IX_MangaConnectorToChapter_ObjId",
                table: "MangaConnectorToChapter",
                column: "ObjId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaConnectorToManga_ObjId",
                table: "MangaConnectorToManga",
                column: "ObjId");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_LibraryId",
                table: "Mangas",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaTagToManga_MangaIds",
                table: "MangaTagToManga",
                column: "MangaIds");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataEntries_MangaId",
                table: "MetadataEntries",
                column: "MangaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AltTitle");

            migrationBuilder.DropTable(
                name: "AuthorToManga");

            migrationBuilder.DropTable(
                name: "Link");

            migrationBuilder.DropTable(
                name: "MangaConnector");

            migrationBuilder.DropTable(
                name: "MangaConnectorToChapter");

            migrationBuilder.DropTable(
                name: "MangaConnectorToManga");

            migrationBuilder.DropTable(
                name: "MangaTagToManga");

            migrationBuilder.DropTable(
                name: "MetadataEntries");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "MetadataFetcher");

            migrationBuilder.DropTable(
                name: "Mangas");

            migrationBuilder.DropTable(
                name: "FileLibraries");
        }
    }
}

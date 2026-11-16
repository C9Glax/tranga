using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.Manga.Database.Migrations
{
    /// <summary>
    /// Suwayomi support was removed, so the download links that point at one of its connectors can never be used again.
    /// This deletes them: the manga's matches, the candidate links themselves and the chapter links that never produced
    /// a file. Mangas, chapters, metadata and downloaded files are left untouched, and so are chapter links that already
    /// hold a file (they are what marks the chapter as downloaded).
    /// </summary>
    /// <remarks>
    /// The identifier of a Suwayomi connector was a name-based UUID (version 5) derived from its Tachiyomi source id, so
    /// there is no list to join against, but the version digit gives it away: no other download extension uses a version 5
    /// UUID (MangaDex is version 7, MANGA Plus and the custom sources are version 4).
    /// </remarks>
    public partial class RemoveSuwayomiDownloadLinks : Migration
    {
        // The version of a UUID is the first character of its third group: xxxxxxxx-xxxx-Vxxx-xxxx-xxxxxxxxxxxx
        private const string IsSuwayomiConnector = "substr(\"DownloadExtension\"::text, 15, 1) = '5'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"DELETE FROM \"MangaDownloadLinks\" WHERE \"DownloadLinkId\" IN (SELECT \"DownloadLinkId\" FROM \"DownloadLinks\" WHERE {IsSuwayomiConnector});");
            migrationBuilder.Sql($"DELETE FROM \"DownloadLinks\" WHERE {IsSuwayomiConnector};");
            migrationBuilder.Sql($"DELETE FROM \"ChapterDownloadLinks\" WHERE {IsSuwayomiConnector} AND \"FileId\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The deleted rows cannot be restored (and the connectors they pointed at no longer exist).
        }
    }
}

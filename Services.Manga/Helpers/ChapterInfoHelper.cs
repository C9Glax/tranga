using Extensions.Data;
using Services.Manga.Database;

namespace Services.Manga.Helpers;

/// <summary>
/// Conversion helpers between download-link database entities and the <see cref="ChapterInfo"/> extension data type.
/// </summary>
public static class ChapterInfoHelper
{
    /// <summary>Converts a chapter download link database entity into a <see cref="ChapterInfo"/>.</summary>
    public static ChapterInfo ToChapterInfo(this DbChapterDownloadLink link) =>
        new(link.DownloadExtension, string.Empty, link.Url ?? string.Empty, link.Identifier);
}

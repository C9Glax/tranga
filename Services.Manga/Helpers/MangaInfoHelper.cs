using Extensions.Data;
using Services.Manga.Database;

namespace Services.Manga.Helpers;

public static class ChapterInfoHelper
{
    public static ChapterInfo ToChapterInfo(this DbChapterDownloadLink link) =>
        new(link.DownloadExtension, string.Empty, link.Url ?? string.Empty, link.Identifier);
}
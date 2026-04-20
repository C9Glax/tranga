using Extensions.Data;
using Services.Manga.Database;

namespace Services.Manga.Helpers;

public static class MangaInfoHelper
{
    public static MangaInfo ToMangaInfo(this DbDownloadLink link) => new(link.DownloadExtension, string.Empty,
        link.Url ?? string.Empty, link.Identifier, default);

    public static DbChapter ToChapter(this ChapterInfo info, DbManga manga) => new()
    {
        ChapterId = Guid.CreateVersion7(),
        MangaId = manga.MangaId,
        Manga = manga,
        Volume = info.Volume,
        Number = info.Number,
        Title = info.Title,
        DownloadLinks = []
    };
    
    public static DbChapterDownloadLink ToChapterDownloadLink(this ChapterInfo info, DbChapter chapter) => new()
    {
        ChapterId = chapter.ChapterId,
        Chapter = chapter,
        DownloadExtension = info.ExtensionIdentifier,
        Identifier = info.Identifier,
        Url = info.Url,
        Priority = 0
    };

    public static DbChapter CreateAndAddChapterDownloadLink(this DbChapter chapter, ChapterInfo info)
    {
        chapter.DownloadLinks ??= [];
        chapter.DownloadLinks.Add(info.ToChapterDownloadLink(chapter));
        return chapter;
    }
}
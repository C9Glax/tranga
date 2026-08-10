using Extensions.Data;
using Services.Manga.Database;

namespace Services.Manga.Helpers;

/// <summary>
/// Conversion helpers between download-link/chapter database entities and the extension data types (<see cref="MangaInfo"/>/<see cref="ChapterInfo"/>).
/// </summary>
public static class MangaInfoHelper
{
    /// <summary>Converts a download link database entity into a <see cref="MangaInfo"/>, without cover/description data.</summary>
    public static MangaInfo ToMangaInfo(this DbDownloadLink link) => new(link.DownloadExtension, string.Empty,
        link.Url ?? string.Empty, link.Identifier, default);

    /// <summary>Creates a new <see cref="DbChapter"/> for the given manga from extension-provided chapter info.</summary>
    /// <param name="info">The chapter info returned by a download extension.</param>
    /// <param name="manga">The manga the chapter belongs to.</param>
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

    /// <summary>Creates a new <see cref="DbChapterDownloadLink"/> for the given chapter from extension-provided chapter info.</summary>
    /// <param name="info">The chapter info returned by a download extension.</param>
    /// <param name="chapter">The chapter the download link belongs to.</param>
    public static DbChapterDownloadLink ToChapterDownloadLink(this ChapterInfo info, DbChapter chapter) => new()
    {
        ChapterId = chapter.ChapterId,
        Chapter = chapter,
        DownloadExtension = info.ExtensionIdentifier,
        Identifier = info.Identifier,
        Url = info.Url,
        Priority = 0
    };

    /// <summary>
    /// Converts the given chapter info into a download link and appends it to the chapter's <see cref="DbChapter.DownloadLinks"/> collection.
    /// </summary>
    /// <param name="chapter">The chapter to add the download link to.</param>
    /// <param name="info">The chapter info returned by a download extension.</param>
    /// <returns>The same chapter instance, with the new download link added.</returns>
    public static DbChapter CreateAndAddChapterDownloadLink(this DbChapter chapter, ChapterInfo info)
    {
        chapter.DownloadLinks ??= [];
        chapter.DownloadLinks.Add(info.ToChapterDownloadLink(chapter));
        return chapter;
    }
}

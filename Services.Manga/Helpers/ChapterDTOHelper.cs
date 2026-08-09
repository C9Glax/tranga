using Services.Manga.Database;
using Services.Manga.Entities;

namespace Services.Manga.Helpers;

internal static class ChapterDTOHelper
{
    public static MangaChapter ToDTO(this DbChapter chapter) => new()
    {
        ChapterId = chapter.ChapterId,
        MangaId = chapter.MangaId,
        Title = chapter.Title,
        Volume = chapter.Volume,
        Number = chapter.Number,
        ReleaseDate = chapter.ReleaseDate,
        IsDownloaded = chapter.DownloadLinks?.Any(l => l.FileId != null) ?? false
    };
}

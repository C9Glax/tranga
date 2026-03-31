using API.DTOs;
using Database.MangaContext;

namespace API.Helpers;

internal static class DbChapterHelpers
{
    public static ChapterDTO ToDTO(this DbChapter chapter) => new()
    {
        ChapterId = chapter.Id,
        DownloadLinkId = chapter.DownloadLinkId,
        DownloadExtensionId = chapter.DownloadExtensionId,
        Identifier = chapter.Identifier,
        Volume = chapter.Volume,
        Chapter = chapter.Chapter,
        Url = chapter.Url,
        Download = chapter.Download,
        FileId = chapter.FileId
    };
}
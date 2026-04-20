using Services.Manga.Database;

namespace Services.Tasks.Helpers;

internal static class ChapterFileHelper
{
    public static string CreateFileName(this DbChapter chapter) => $"Vol. {chapter.Volume ?? "0"} Ch. {chapter.Number} - {chapter.Title}.cbz"; // TODO: REPLACE WITH REGEX
}
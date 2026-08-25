using Common.Datatypes;
using Services.Manga.Database;

namespace Services.Tasks.Helpers;

/// <summary>
/// Builds <see cref="ComicInfo"/> metadata for a chapter, so readers like Komga can order chapters by
/// <see cref="ComicInfo.Volume"/>/<see cref="ComicInfo.Number"/> instead of falling back to filename sorting
/// (which misorders volumed and volume-less chapters relative to each other).
/// </summary>
public static class ComicInfoHelper
{
    public static ConcreteComicInfo CreateComicInfo(this DbChapter chapter, string seriesName) => new()
    {
        Series = seriesName,
        Title = chapter.Title ?? string.Empty,
        Number = chapter.Number,
        Volume = int.TryParse(chapter.Volume, out int volume) ? volume : -1,
        Manga = Common.Datatypes.Manga.Yes,
    };
}

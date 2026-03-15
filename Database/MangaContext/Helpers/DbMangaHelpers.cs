using Common.Datatypes;

namespace Database.MangaContext.Helpers;

public static class DbMangaHelpers
{
    public static SearchQuery ToSearchQuery(this DbManga manga) =>
        (manga.ComicInfo?.ToSearchQuery() ?? new SearchQuery()) with
        {
            MangaUpdatesSeriesId = manga.MangaUpdatesSeriesId
        };
}
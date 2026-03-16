using Common.Datatypes;

namespace Database.MangaContext.Helpers;

public static class DbMangaHelpers
{
    public static SearchQuery ToSearchQuery(this DbManga manga) => new ()
        {
            MangaUpdatesSeriesId = manga.MangaUpdatesSeriesId,
            Title = manga.Title,
            // TODO
        };
}
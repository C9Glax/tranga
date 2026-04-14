using Common.Datatypes;
using Services.Manga.Database;

namespace Services.Manga.Helpers;

public static class SearchQueryHelper
{
    public static SearchQuery ToSearchQuery(this DbMangaMetadataEntries source) => new()
    {
        Title = source.Metadata.Series
        //TODO Add more fields
    };
}
using Common.Datatypes;
using Database.MangaContext;

namespace API.Helpers;

public static class SearchQueryHelper
{
    public static SearchQuery ToSearchQuery(this DbMangaMetadataSource source) => new()
    {
        Title = source.MetadataSource.Series
        //TODO Add more fields
    };
}
namespace Common.Datatypes;

public static class ComicInfoHelpers
{
    public static SearchQuery ToSearchQuery(this ComicInfo comicInfo) => new SearchQuery()
    {
        Title = comicInfo.Title,
    };
}
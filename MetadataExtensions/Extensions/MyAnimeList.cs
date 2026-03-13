using Common.Data;
using Data;

namespace MetadataExtensions.Extensions;

public class MyAnimeList : IMetadataExtension
{
    public Task<List<ComicInfo>?> Search(SearchQuery searchQuery)
    {
        throw new NotImplementedException();
    }
}
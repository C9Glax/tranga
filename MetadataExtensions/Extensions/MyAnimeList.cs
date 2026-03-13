using Common.Data;
using Data;

namespace MetadataExtensions.Extensions;

public class MyAnimeList : IMetadataExtension
{
    public string BaseUrl { get; init; } = "https://api.myanimelist.net/";

    public Task<List<ComicInfo>?> Search(SearchQuery searchQuery, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
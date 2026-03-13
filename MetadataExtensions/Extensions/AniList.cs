using Common.Data;
using Data;

namespace MetadataExtensions.Extensions;

public class AniList : IMetadataExtension
{
    public string BaseUrl { get; init; } = "https://graphql.anilist.co";

    public Task<List<ComicInfo>?> Search(SearchQuery searchQuery, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
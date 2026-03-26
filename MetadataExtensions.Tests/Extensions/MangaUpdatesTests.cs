using Common.Datatypes;
using MetadataExtensions.Extensions;

namespace MetadataExtensions.Tests.Extensions;

public sealed class MangaUpdatesTests : IMetadataExtensionTests<MangaUpdates>
{
    [Fact]
    public async Task SearchReturnsManga()
    {
        SearchQuery searchQuery = new()
        {
            Title = "Sousou no Frieren"
        };
        List<SearchResult>? result = await _metadataExtension.Search(searchQuery, ct);
        Assert.NotNull(result);
        SearchResult? manga = result.FirstOrDefault(r => r.Url == "https://www.mangaupdates.com/series/ugf5dzu/sousou-no-frieren");
        Assert.NotNull(manga);
    }
}
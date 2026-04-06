using Common.Datatypes;
using Extensions.Data;
using Extensions.Extensions;

namespace Extensions.Tests.Extensions;

public sealed class MangaUpdatesTests : ExtensionTests<MangaUpdates>
{
    [Fact]
    public async Task SearchReturnsManga()
    {
        SearchQuery searchQuery = new()
        {
            Title = "Sousou no Frieren"
        };
        List<SearchResult>? result = await _extension.SearchMetadata(searchQuery, ct);
        Assert.NotNull(result);
        SearchResult? manga = result.FirstOrDefault(r => r.Url == "https://www.mangaupdates.com/series/ugf5dzu/sousou-no-frieren");
        Assert.NotNull(manga);
    }
}
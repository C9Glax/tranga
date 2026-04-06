using Common.Datatypes;
using MetadataExtensions.Extensions;

namespace MetadataExtensions.Tests.Extensions;

public sealed class MangaDexTests : IMetadataExtensionTests<MangaDex>
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
        SearchResult? manga = result.FirstOrDefault(r => r.Url == "https://mangadex.org/title/b0b721ff-c388-4486-aa0f-c2b0bb321512");
        Assert.NotNull(manga);
    }

    [Fact]
    public async Task IdReturnsManga()
    {
        SearchQuery searchQuery = new()
        {
            MangaDexSeriesId = Guid.Parse("b0b721ff-c388-4486-aa0f-c2b0bb321512")
        };
        List<SearchResult>? result = await _metadataExtension.Search(searchQuery, ct);
        Assert.NotNull(result);
        SearchResult? manga = result.FirstOrDefault(r => r.Url == "https://mangadex.org/title/b0b721ff-c388-4486-aa0f-c2b0bb321512");
        Assert.NotNull(manga);
    }
}
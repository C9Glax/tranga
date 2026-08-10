using Common.Datatypes;
using Extensions.Data;
using Extensions.Extensions;

namespace Extensions.Tests;

public sealed class MetadataExtensionCollectionTests : Common.Tests.TrangaTest
{
    [Fact]
    public void UniqueExtensionIds()
    {
        Assert.Distinct(MetadataExtensionsCollection.Extensions.Select(e => e.Identifier));
    }

    [Fact]
    public async Task SearchDefaultsToDownloadLanguageWhenQueryHasNoLanguage()
    {
        // Individual extensions should never see a null Language - Search fills it in from
        // Settings.DownloadLanguage before dispatching, so every extension defaults consistently.
        SearchQuery searchQuery = new()
        {
            Title = "Sousou no Frieren"
        };
        List<SearchResult> result = MetadataExtensionsCollection.Search(searchQuery, [new MangaDex()], ct);
        SearchResult? manga = result.FirstOrDefault(r => r.Url == "https://mangadex.org/title/b0b721ff-c388-4486-aa0f-c2b0bb321512");
        Assert.NotNull(manga);
        Assert.Equal("Sousou no Frieren", manga.Series);
    }
}
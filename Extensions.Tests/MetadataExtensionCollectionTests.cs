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
    public void MyAnimeListIsOnlyRegisteredWhenClientIdIsConfigured()
    {
        // The MyAnimeList API answers 403 without a client-id, so an unconfigured instance would be a provider that
        // can never return anything - it must not be offered at all.
        bool registered = MetadataExtensionsCollection.Extensions.Any(e => e.Identifier == new MyAnimeList().Identifier);

        Assert.Equal(Common.Settings.EnvVars.MAL_CLIENT_ID is not null, registered);
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
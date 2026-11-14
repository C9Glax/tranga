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

    [Theory]
    [InlineData("https://www.mangaupdates.com/series/ugf5dzu/sousou-no-frieren", typeof(MangaUpdates))]
    [InlineData("https://mangadex.org/title/b0b721ff-c388-4486-aa0f-c2b0bb321512", typeof(MangaDex))]
    [InlineData("https://anilist.co/manga/118586", typeof(AniList))]
    public void ParseUrlMatchesOnlyTheOwningExtension(string url, Type extensionType)
    {
        (IMetadataExtension Extension, SearchQuery Query) match = Assert.Single(MetadataExtensionsCollection.ParseUrl(url));
        Assert.IsType(extensionType, match.Extension);
    }

    [Fact]
    public void ParseUrlMatchesNothingForUnknownSite() =>
        Assert.Empty(MetadataExtensionsCollection.ParseUrl("https://example.com/manga/118586"));

    [Fact]
    public void SearchSkipsExtensionsThatThrow()
    {
        List<SearchResult> result = MetadataExtensionsCollection.Search(new SearchQuery(Title: "anything"), [new ThrowingExtension()], ct);
        Assert.Empty(result);
    }

    private sealed class ThrowingExtension : IMetadataExtension
    {
        public Guid Identifier { get; init; } = Guid.NewGuid();
        public string Name { get; init; } = "Throwing";
        public string BaseUrl { get; init; } = "https://example.com";
        public string IconUrl { get; init; } = "https://example.com/icon.png";

        public Task<List<SearchResult>?> SearchMetadata(SearchQuery searchQuery, CancellationToken ct) =>
            throw new HttpRequestException("boom");

        public SearchQuery? ParseSearchQueryFromUrl(string url) => null;
    }
}

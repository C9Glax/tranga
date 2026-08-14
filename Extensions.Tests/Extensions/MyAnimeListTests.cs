using Common.Datatypes;
using Common.Settings;
using Extensions.Data;
using Extensions.Extensions;

namespace Extensions.Tests.Extensions;

public sealed class MyAnimeListTests : ExtensionTests<MyAnimeList>
{
    private const int FrierenMalId = 126287;

    /// <summary>
    /// The MyAnimeList API answers 403 without a client-id, so the network tests can only run where
    /// <c>MAL_CLIENT_ID</c> is configured. Everywhere else (CI) they skip instead of failing.
    /// </summary>
    private static void SkipWithoutClientId() =>
        Assert.SkipWhen(EnvVars.MAL_CLIENT_ID is null, "MAL_CLIENT_ID is not set");

    [Fact]
    public async Task SearchReturnsNullWithoutClientId()
    {
        Assert.SkipWhen(EnvVars.MAL_CLIENT_ID is not null, "MAL_CLIENT_ID is set");

        List<SearchResult>? result = await _extension.SearchMetadata(new SearchQuery { Title = "Frieren" }, ct);
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchReturnsManga()
    {
        SkipWithoutClientId();

        SearchQuery searchQuery = new()
        {
            Title = "Sousou no Frieren"
        };
        List<SearchResult>? result = await _extension.SearchMetadata(searchQuery, ct);
        Assert.NotNull(result);
        SearchResult? manga = result.FirstOrDefault(r => r.Url == $"https://myanimelist.net/manga/{FrierenMalId}");
        Assert.NotNull(manga);
        Assert.True(manga.Cover.Length > 0);
    }

    [Fact]
    public async Task SearchByIdReturnsExactManga()
    {
        SkipWithoutClientId();

        SearchQuery searchQuery = new()
        {
            MyAnimeListSeriesId = FrierenMalId
        };
        List<SearchResult>? result = await _extension.SearchMetadata(searchQuery, ct);
        Assert.NotNull(result);
        SearchResult manga = Assert.Single(result);
        Assert.Equal(FrierenMalId.ToString(), manga.Identifier);
        Assert.Equal(2020, manga.Year);
    }

    [Fact]
    public async Task SearchIgnoresTooShortQuery()
    {
        SkipWithoutClientId();

        // MyAnimeList rejects q shorter than 3 characters, so the extension must not even send the request
        List<SearchResult>? result = await _extension.SearchMetadata(new SearchQuery { Title = "ab" }, ct);
        Assert.Null(result);
    }
}

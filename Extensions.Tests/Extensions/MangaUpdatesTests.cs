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

    private const long FrierenMangaUpdatesId = 66296374554;
    private const string FrierenUrl = "https://www.mangaupdates.com/series/ugf5dzu/sousou-no-frieren";

    [Theory]
    [InlineData(FrierenUrl)]
    [InlineData("https://www.mangaupdates.com/series/ugf5dzu")]
    [InlineData("https://mangaupdates.com/series/UGF5DZU/")]
    [InlineData("  https://WWW.MangaUpdates.com/series/ugf5dzu/sousou-no-frieren?tab=info  ")]
    public void ParseSearchQueryFromUrlDecodesBase36SeriesId(string url)
    {
        SearchQuery? query = _extension.ParseSearchQueryFromUrl(url);
        Assert.NotNull(query);
        Assert.Equal(FrierenMangaUpdatesId, query.MangaUpdatesSeriesId);
        Assert.Null(query.Title);
    }

    [Fact]
    public void ParseSearchQueryFromUrlDecodesKnownSeriesId() =>
        Assert.Equal(55099564912, _extension.ParseSearchQueryFromUrl("https://www.mangaupdates.com/series/pb8uwds/berserk")?.MangaUpdatesSeriesId);

    [Theory]
    [InlineData("https://www.mangaupdates.com/series.html?id=88")]
    [InlineData("https://www.mangaupdates.com/group/abc123/some-group")]
    [InlineData("https://www.mangaupdates.com/series/not-base36!")]
    [InlineData("https://www.mangaupdates.com/series/zzzzzzzzzzzzzz")]
    [InlineData("https://mangadex.org/series/ugf5dzu")]
    [InlineData("ftp://www.mangaupdates.com/series/ugf5dzu")]
    [InlineData("Sousou no Frieren")]
    public void ParseSearchQueryFromUrlRejectsOtherUrls(string url) =>
        Assert.Null(_extension.ParseSearchQueryFromUrl(url));

    [Fact]
    public async Task SearchByUrlReturnsExactManga()
    {
        SearchQuery query = _extension.ParseSearchQueryFromUrl(FrierenUrl)!;
        List<SearchResult>? result = await _extension.SearchMetadata(query, ct);
        Assert.NotNull(result);
        SearchResult manga = Assert.Single(result);
        Assert.Equal(FrierenMangaUpdatesId.ToString(), manga.Identifier);
    }

    [Fact]
    public async Task SearchByUnknownIdReturnsNull()
    {
        List<SearchResult>? result = await _extension.SearchMetadata(new SearchQuery(MangaUpdatesSeriesId: 1), ct);
        Assert.Null(result);
    }
}

using Common.Datatypes;
using Extensions.Data;
using Extensions.Extensions;

namespace Extensions.Tests.Extensions;

public sealed class AniListTests : ExtensionTests<AniList>
{
    private const int FrierenAniListId = 118586;
    private const string FrierenUrl = "https://anilist.co/manga/118586";

    [Fact]
    public async Task SearchReturnsManga()
    {
        SearchQuery searchQuery = new()
        {
            Title = "Sousou no Frieren"
        };
        List<SearchResult>? result = await _extension.SearchMetadata(searchQuery, ct);
        Assert.NotNull(result);
        SearchResult? manga = result.FirstOrDefault(r => r.Url == FrierenUrl);
        Assert.NotNull(manga);
        Assert.NotEmpty(manga.Series);
        Assert.True(manga.Cover.Length > 0);
    }

    [Fact]
    public async Task SearchByIdReturnsExactManga()
    {
        SearchQuery searchQuery = new()
        {
            AniListSeriesId = FrierenAniListId
        };
        List<SearchResult>? result = await _extension.SearchMetadata(searchQuery, ct);
        Assert.NotNull(result);
        SearchResult manga = Assert.Single(result);
        Assert.Equal(FrierenAniListId.ToString(), manga.Identifier);
        Assert.Equal(FrierenUrl, manga.Url);
        Assert.Equal(2020, manga.Year);
        Assert.Contains("Kanehito Yamada", manga.Authors ?? []);
        // Descriptions arrive with inline markup that has to be stripped before it reaches the frontend
        Assert.DoesNotContain("<br>", manga.Summary ?? string.Empty);
    }
}

using Common.Datatypes;
using DownloadExtensions.Data;
using DownloadExtensions.Extensions;

namespace DownloadExtensions.Tests.Extensions;

public sealed class MangaDexTests : IDownloadExtensionsTests<MangaDex>
{
    [Fact]
    public async Task SearchReturnsManga()
    {
        // https://mangadex.org/title/f9c33607-9180-4ba6-b85c-e4b5faee7192/official-test-manga
        SearchQuery searchQuery = new()
        {
            Title = "Official \"Test\" Manga"
        };
        MangaSearchResult<MangaDex>? searchResult = await _downloadExtension.Search(searchQuery, ct);
        Assert.NotNull(searchResult);
        MangaInfo<MangaDex>? testManga = searchResult.FirstOrDefault(r => r.Identifier == "f9c33607-9180-4ba6-b85c-e4b5faee7192");
        Assert.NotNull(testManga);
        Assert.Equal("Official \"Test\" Manga", testManga.Title);
        Assert.False(string.IsNullOrEmpty(testManga.Description));
        Assert.Equal("https://mangadex.org/title/f9c33607-9180-4ba6-b85c-e4b5faee7192", testManga.Url);
    }
}
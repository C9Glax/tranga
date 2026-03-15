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
        SearchQuery searchQuery = new ()
        {
            Title = "Official \"Test\" Manga"
        };
        MangaSearchResult? searchResult = await _downloadExtension.Search(searchQuery, ct);
        Assert.NotNull(searchResult);
        MangaInfo? testManga = searchResult.FirstOrDefault(r => r.Identifier == "f9c33607-9180-4ba6-b85c-e4b5faee7192");
        Assert.NotNull(testManga);
        Assert.Equal("Official \"Test\" Manga", testManga.Title);
        Assert.False(string.IsNullOrEmpty(testManga.Description));
        Assert.Equal("https://mangadex.org/title/f9c33607-9180-4ba6-b85c-e4b5faee7192", testManga.Url);
        Assert.True(testManga.Cover.Length > 0);
    }

    [Fact]
    public async Task ChapterRetrievalReturnsChapters()
    {
        // https://mangadex.org/title/f9c33607-9180-4ba6-b85c-e4b5faee7192/official-test-manga
        // https://mangadex.org/chapter/249f2aa4-38a9-428f-8632-9a4aecc013ad
        MangaInfo mangaInfo = new (
            _downloadExtension.Identifier,
            "Official \"Test\" Manga",
            "https://mangadex.org/title/f9c33607-9180-4ba6-b85c-e4b5faee7192",
            "f9c33607-9180-4ba6-b85c-e4b5faee7192",
            new MemoryStream()
        );
        List<ChapterInfo>? chapters = await _downloadExtension.GetChapters(mangaInfo, ct);
        Assert.NotNull(chapters);
        Assert.NotEmpty(chapters);
        Assert.True(chapters.Count > 100); // multiple pages
        Assert.Contains("249f2aa4-38a9-428f-8632-9a4aecc013ad", chapters.Select(c => c.Identifier));
    }
    

    [Fact]
    public async Task ChapterImagesReturnsImage()
    {
        // https://mangadex.org/chapter/249f2aa4-38a9-428f-8632-9a4aecc013ad
        ChapterInfo mangaInfo = new (
            _downloadExtension.Identifier,
            string.Empty,
            "https://mangadex.org/chapter/249f2aa4-38a9-428f-8632-9a4aecc013ad",
            "249f2aa4-38a9-428f-8632-9a4aecc013ad"
        );
        List<ChapterImage>? images = await _downloadExtension.GetChapterImages(mangaInfo, ct);
        Assert.NotNull(images);
        Assert.Single(images);
        Assert.Equal("249f2aa4-38a9-428f-8632-9a4aecc013ad", images.First().chapterIdentifier);
        Assert.Equal(0, images.First().order);
    }
}
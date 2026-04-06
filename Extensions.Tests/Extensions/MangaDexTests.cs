using Common.Datatypes;
using Extensions.Data;
using Extensions.Extensions;

namespace Extensions.Tests.Extensions;

public sealed class MangaDexTests : DownloadExtensionTests<MangaDex>
{
    [Fact]
    public async Task MetadataSearchReturnsManga()
    {
        // https://mangadex.org/title/f9c33607-9180-4ba6-b85c-e4b5faee7192/official-test-manga
        SearchQuery searchQuery = new ()
        {
            Title = "Official \"Test\" Manga"
        };
        List<MangaInfo>? searchResult = await _extension.SearchDownload(searchQuery, ct);
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
            _extension.Identifier,
            "Official \"Test\" Manga",
            "https://mangadex.org/title/f9c33607-9180-4ba6-b85c-e4b5faee7192",
            "f9c33607-9180-4ba6-b85c-e4b5faee7192",
            new MemoryStream()
        );
        List<ChapterInfo>? chapters = await _extension.GetChapters(mangaInfo, ct);
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
            _extension.Identifier,
            string.Empty,
            "https://mangadex.org/chapter/249f2aa4-38a9-428f-8632-9a4aecc013ad",
            "249f2aa4-38a9-428f-8632-9a4aecc013ad"
        );
        List<ChapterImage>? images = await _extension.GetChapterImages(mangaInfo, ct);
        Assert.NotNull(images);
        Assert.Single(images);
        Assert.Equal("249f2aa4-38a9-428f-8632-9a4aecc013ad", images.First().chapterIdentifier);
        Assert.Equal(0, images.First().order);
    }
    
    [Fact]
    public async Task DownloadSearchReturnsManga()
    {
        SearchQuery searchQuery = new()
        {
            Title = "Sousou no Frieren"
        };
        List<SearchResult>? result = await _extension.SearchMetadata(searchQuery, ct);
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
        List<SearchResult>? result = await _extension.SearchMetadata(searchQuery, ct);
        Assert.NotNull(result);
        SearchResult? manga = result.FirstOrDefault(r => r.Url == "https://mangadex.org/title/b0b721ff-c388-4486-aa0f-c2b0bb321512");
        Assert.NotNull(manga);
    }
}
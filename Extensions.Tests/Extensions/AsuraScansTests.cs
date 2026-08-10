using Common.Datatypes;
using Extensions.Data;
using Extensions.Extensions;

namespace Extensions.Tests.Extensions;

public sealed class AsuraScansTests : DownloadExtensionTests<AsuraScans>
{
    [Fact]
    public async Task SearchReturnsManga()
    {
        SearchQuery searchQuery = new()
        {
            Title = "Solo Leveling: Ragnarok"
        };
        List<MangaInfo>? searchResult = await _extension.SearchDownload(searchQuery, ct);
        Assert.NotNull(searchResult);
        MangaInfo? manga = searchResult.FirstOrDefault(r => r.Identifier == "solo-leveling-ragnarok");
        Assert.NotNull(manga);
        Assert.Equal("Solo Leveling: Ragnarok", manga.Title);
        Assert.True(manga.Cover.Length > 0);
    }

    [Fact]
    public async Task ChapterRetrievalReturnsChapters()
    {
        MangaInfo mangaInfo = new(
            _extension.Identifier,
            "Solo Leveling: Ragnarok",
            "https://asurascans.com/comics/solo-leveling-ragnarok",
            "solo-leveling-ragnarok",
            new Common.Helpers.TrangaImage()
        );
        List<ChapterInfo>? chapters = await _extension.GetChapters(mangaInfo, ct);
        Assert.NotNull(chapters);
        Assert.True(chapters.Count > 60);
        Assert.Contains("1", chapters.Select(c => c.Number));
    }

    [Fact]
    public async Task ChapterImagesReturnsImages()
    {
        ChapterInfo chapterInfo = new(
            _extension.Identifier,
            "1",
            "https://asurascans.com/comics/solo-leveling-ragnarok/chapter/1",
            "0ef51071-9881-48fc-9650-738435a566b8"
        );
        List<ChapterImage>? images = await _extension.FetchChapterImages(chapterInfo, ct);
        Assert.NotNull(images);
        Assert.NotEmpty(images);
        Assert.All(images, i => Assert.Equal("0ef51071-9881-48fc-9650-738435a566b8", i.chapterIdentifier));
    }

    [Fact]
    public void ParseIdentifierFromUrlReturnsSlugForComicsPage()
    {
        Assert.Equal("solo-leveling-ragnarok", _extension.ParseIdentifierFromUrl("https://asurascans.com/comics/solo-leveling-ragnarok"));
    }

    [Fact]
    public void ParseIdentifierFromUrlReturnsNullForForeignSite()
    {
        Assert.Null(_extension.ParseIdentifierFromUrl("https://mangadex.org/title/f9c33607-9180-4ba6-b85c-e4b5faee7192"));
    }
}

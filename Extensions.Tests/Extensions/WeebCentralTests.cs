using Common.Datatypes;
using Extensions.Data;
using Extensions.Extensions;

namespace Extensions.Tests.Extensions;

public sealed class WeebCentralTests : DownloadExtensionTests<WeebCentral>
{
    [Fact]
    public async Task SearchReturnsManga()
    {
        SearchQuery searchQuery = new()
        {
            Title = "Tower of God"
        };
        List<MangaInfo>? searchResult = await _extension.SearchDownload(searchQuery, ct);
        Assert.NotNull(searchResult);
        MangaInfo? manga = searchResult.FirstOrDefault(r => r.Identifier == "01J76XY7M0W9WWJ55VJYYB2J1S");
        Assert.NotNull(manga);
        Assert.Equal("Tower of God", manga.Title);
        Assert.Equal("https://weebcentral.com/series/01J76XY7M0W9WWJ55VJYYB2J1S/Tower-Of-God", manga.Url);
        Assert.True(manga.Cover.Length > 0);
    }

    [Fact]
    public async Task ChapterRetrievalReturnsChapters()
    {
        MangaInfo mangaInfo = new(
            _extension.Identifier,
            "Tower of God",
            "https://weebcentral.com/series/01J76XY7M0W9WWJ55VJYYB2J1S/Tower-Of-God",
            "01J76XY7M0W9WWJ55VJYYB2J1S",
            new Common.Helpers.TrangaImage()
        );
        List<ChapterInfo>? chapters = await _extension.GetChapters(mangaInfo, ct);
        Assert.NotNull(chapters);
        Assert.True(chapters.Count > 200); // long-running series
        Assert.Contains("01JP5ZTDW79G1DDKCXNR3W25Y4", chapters.Select(c => c.Identifier));
    }

    [Fact]
    public async Task ChapterImagesReturnsImages()
    {
        ChapterInfo chapterInfo = new(
            _extension.Identifier,
            "8.5",
            "https://weebcentral.com/chapters/01KB7YS3E5X7E58XDG1985CSAP",
            "01KB7YS3E5X7E58XDG1985CSAP"
        );
        List<ChapterImage>? images = await _extension.FetchChapterImages(chapterInfo, ct);
        Assert.NotNull(images);
        Assert.NotEmpty(images);
        Assert.All(images, i => Assert.Equal("01KB7YS3E5X7E58XDG1985CSAP", i.chapterIdentifier));
    }
}

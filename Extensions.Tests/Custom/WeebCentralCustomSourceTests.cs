using Common.Datatypes;
using Common.Helpers;
using Extensions.Custom;
using Extensions.Data;

namespace Extensions.Tests.Custom;

/// <summary>
/// The WeebCentral example definition (a port of the keiyoushi <c>weebcentral</c> extension) run against the real
/// website. Exercises relative URLs, a chapter list that lives at a different URL than the series page, srcset
/// fallbacks and regex replacements end to end.
/// </summary>
public sealed class WeebCentralCustomSourceTests : Common.Tests.TrangaTest, IDisposable
{
    private const string OnePieceUrl = "https://weebcentral.com/series/01J76XY7E9FNDZ1DBBM6PBJPFK/One-Piece";

    private readonly SelectorDownloadExtension _extension = new(
        SelectorDownloaderJson.WeebCentralExample() with { Id = Guid.NewGuid() },
        new SelectorDownloaderOptions(AllowPrivateHosts: false, FlareSolverrUrl: null));

    public void Dispose() => _extension.Dispose();

    [Fact]
    public void ExampleDefinitionIsValid() =>
        Assert.Empty((SelectorDownloaderJson.WeebCentralExample() with { Id = Guid.NewGuid() }).Validate());

    [Fact]
    public async Task TestSearchReportsWhatWasExtracted()
    {
        StepTestResult result = await _extension.TestSearchAsync("one piece", ct);

        Assert.Null(result.Error);
        Assert.Equal(200, result.StatusCode);
        Assert.True(result.ItemCount > 0);
        Assert.Contains(result.Rows, r => r["url"] == OnePieceUrl && r["title"] == "One Piece");
    }

    [Fact]
    public async Task SearchFindsOnePiece()
    {
        List<MangaInfo>? result = await _extension.SearchDownload(new SearchQuery { Title = "one piece", Language = "en" }, ct);

        Assert.NotNull(result);
        MangaInfo manga = Assert.Single(result, m => m.Url == OnePieceUrl);
        Assert.Equal("One Piece", manga.Title);
        Assert.Equal(OnePieceUrl, manga.Identifier);
        Assert.True(manga.Cover.Length > 0);
        Assert.Equal(_extension.Identifier, manga.ExtensionIdentifier);
    }

    [Fact]
    public async Task ChaptersAreListedFromTheFullChapterListPage()
    {
        MangaInfo manga = new(_extension.Identifier, "One Piece", OnePieceUrl, OnePieceUrl, new TrangaImage());

        List<ChapterInfo>? chapters = await _extension.GetChapters(manga, ct);

        Assert.NotNull(chapters);
        Assert.True(chapters.Count > 1000, $"expected more than 1000 chapters, got {chapters.Count}");
        Assert.All(chapters, c =>
        {
            Assert.StartsWith("https://weebcentral.com/chapters/", c.Url);
            Assert.Equal(c.Url, c.Identifier);
            Assert.True(decimal.TryParse(c.Number, System.Globalization.CultureInfo.InvariantCulture, out _));
        });
        Assert.Contains(chapters, c => c.Number == "1" && c.ReleaseDate is not null);
    }

    [Fact]
    public async Task ImagesDecodeAsImages()
    {
        MangaInfo manga = new(_extension.Identifier, "One Piece", OnePieceUrl, OnePieceUrl, new TrangaImage());
        List<ChapterInfo>? chapters = await _extension.GetChapters(manga, ct);
        Assert.NotNull(chapters);

        // GetChapterImages re-encodes every page, so anything that is not an image makes it return null.
        List<ChapterImage>? images = await ((IDownloadExtension)_extension).GetChapterImages(chapters.First(c => c.Number == "1"), ct);

        Assert.NotNull(images);
        Assert.NotEmpty(images);
        Assert.Equal(Enumerable.Range(0, images.Count), images.Select(i => i.order));
    }

    [Fact]
    public async Task CoverOfAPastedSeriesUrlIsDownloaded()
    {
        // the series page of a manga whose link is added by pasting its URL: no search result to take the cover from
        string url = "https://weebcentral.com/series/01JGRW9YR8AYQFAMVQ416AY8GX/majutsu-wo-kiwamete-tabi-ni-deta-tensei-elf-moteamashita-jumyou-de-ikeru-densetsu-to-naru";
        MangaInfo manga = new(_extension.Identifier, "", url, _extension.ParseIdentifierFromUrl(url)!, new TrangaImage());

        TrangaImage? cover = await _extension.GetCover(manga, ct);

        Assert.NotNull(cover);
        await cover.ToJpeg(ct); // an image, not an error page
        Assert.True(cover.Length > 0);
    }

    [Fact]
    public void ParseIdentifierFromUrl()
    {
        Assert.Equal(OnePieceUrl, _extension.ParseIdentifierFromUrl(OnePieceUrl));
        Assert.Null(_extension.ParseIdentifierFromUrl("https://weebcentral.com/chapters/01M6RHBP1Y3A03BK0TZMBJ2YZN"));
        Assert.Null(_extension.ParseIdentifierFromUrl("https://example.com/series/01J76XY7E9FNDZ1DBBM6PBJPFK/One-Piece"));
    }
}

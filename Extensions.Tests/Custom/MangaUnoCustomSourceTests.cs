using Common.Datatypes;
using Common.Helpers;
using Extensions.Custom;
using Extensions.Data;

namespace Extensions.Tests.Custom;

/// <summary>
/// The Manga.uno example definition (a port of the keiyoushi <c>mangauno</c> extension) run against the real website.
/// Manga.uno is a JavaScript application on top of a JSON API, so this exercises the JSON format end to end: the search
/// and the chapter list come from the API, the links are built from ids, and the pages are downloaded from the CDN.
/// </summary>
public sealed class MangaUnoCustomSourceTests : Common.Tests.TrangaTest, IDisposable
{
    private const string SeriesUrl = "https://manga.uno/m/one-piece";

    private readonly SelectorDownloadExtension _extension = new(
        SelectorDownloaderJson.MangaUnoExample() with { Id = Guid.NewGuid() },
        new SelectorDownloaderOptions(AllowPrivateHosts: false, FlareSolverrUrl: null));

    public void Dispose() => _extension.Dispose();

    private MangaInfo OnePiece => new(_extension.Identifier, "One Piece", SeriesUrl, SeriesUrl, new TrangaImage());

    [Fact]
    public void ExampleDefinitionIsValid() =>
        Assert.Empty((SelectorDownloaderJson.MangaUnoExample() with { Id = Guid.NewGuid() }).Validate());

    [Fact]
    public async Task SearchFindsTheSeriesAndDownloadsItsCover()
    {
        List<MangaInfo>? result = await _extension.SearchDownload(new SearchQuery { Title = "one piece", Language = "en" }, ct);

        Assert.NotNull(result);
        MangaInfo manga = Assert.Single(result, m => m.Url == SeriesUrl);
        Assert.Equal("One Piece", manga.Title);
        Assert.Equal(SeriesUrl, manga.Identifier);
        Assert.True(manga.Cover.Length > 0);
    }

    [Fact]
    public async Task ChaptersAreListedFromTheApi()
    {
        List<ChapterInfo>? chapters = await _extension.GetChapters(OnePiece, ct);

        Assert.NotNull(chapters);
        Assert.True(chapters.Count > 1000, $"{chapters.Count} chapters");
        ChapterInfo first = Assert.Single(chapters, c => c.Number == "1");
        Assert.StartsWith("https://manga.uno/r/one-piece/", first.Url);
        Assert.All(chapters, c => Assert.Matches(@"^\d+(\.\d*[1-9])?$", c.Number)); // "1193", "1080.1", never "1193.0000"
        Assert.All(chapters, c => Assert.NotNull(c.ReleaseDate));
    }

    [Fact]
    public async Task EveryPageOfAChapterIsDownloaded()
    {
        List<ChapterInfo>? chapters = await _extension.GetChapters(OnePiece, ct);
        Assert.NotNull(chapters);
        ChapterInfo chapter = chapters.Single(c => c.Number == "1");

        // GetChapterImages re-encodes every page, so anything that is not an image makes it return null.
        List<ChapterImage>? images = await ((IDownloadExtension)_extension).GetChapterImages(chapter, ct);

        Assert.NotNull(images);
        Assert.True(images.Count > 10, $"{images.Count} pages");
        Assert.Equal(Enumerable.Range(0, images.Count), images.Select(i => i.order));
    }

    [Fact]
    public void ParseIdentifierFromUrl()
    {
        Assert.Equal(SeriesUrl, _extension.ParseIdentifierFromUrl(SeriesUrl));
        Assert.Null(_extension.ParseIdentifierFromUrl("https://manga.uno/r/one-piece/800778")); // a chapter
        Assert.Null(_extension.ParseIdentifierFromUrl("https://example.com/m/one-piece"));
    }
}

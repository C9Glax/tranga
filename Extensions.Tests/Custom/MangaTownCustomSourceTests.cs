using Common.Datatypes;
using Common.Helpers;
using Extensions.Custom;
using Extensions.Data;

namespace Extensions.Tests.Custom;

/// <summary>
/// The MangaTown example definition (a port of the keiyoushi <c>mangatown</c> extension) run against the real
/// website. Exercises a chapter list on the series page, chapter numbers taken from the URL, and chapters that are
/// spread over one HTML page per image, end to end including the image CDN that requires a Referer.
/// </summary>
public sealed class MangaTownCustomSourceTests : Common.Tests.TrangaTest, IDisposable
{
    private const string SeriesUrl = "https://www.mangatown.com/manga/naruto_dj_family_wedding/";

    private readonly SelectorDownloadExtension _extension = new(
        SelectorDownloaderJson.MangaTownExample() with { Id = Guid.NewGuid() },
        new SelectorDownloaderOptions(AllowPrivateHosts: false, FlareSolverrUrl: null));

    public void Dispose() => _extension.Dispose();

    [Fact]
    public void ExampleDefinitionIsValid() =>
        Assert.Empty((SelectorDownloaderJson.MangaTownExample() with { Id = Guid.NewGuid() }).Validate());

    [Fact]
    public async Task SearchFindsTheSeriesAndDownloadsItsCover()
    {
        List<MangaInfo>? result = await _extension.SearchDownload(new SearchQuery { Title = "family wedding", Language = "en" }, ct);

        Assert.NotNull(result);
        MangaInfo manga = Assert.Single(result, m => m.Url == SeriesUrl);
        Assert.Equal("Naruto dj - Family Wedding", manga.Title);
        Assert.Equal(SeriesUrl, manga.Identifier);
        Assert.True(manga.Cover.Length > 0);
    }

    [Fact]
    public async Task CoverOfAPastedSeriesUrlIsDownloadedFromThePageOfTheSeries()
    {
        // a series that is added by its URL has no search result: the cover is the image of the info block of its page
        MangaInfo manga = new(_extension.Identifier, "", SeriesUrl, SeriesUrl, new TrangaImage());

        TrangaImage? cover = await _extension.GetCover(manga, ct);

        Assert.NotNull(cover);
        await cover.ToJpeg(ct); // an image, not an error page
        Assert.True(cover.Length > 0);
    }

    [Fact]
    public async Task ChaptersAreListedFromTheSeriesPage()
    {
        MangaInfo manga = new(_extension.Identifier, "Naruto dj - Family Wedding", SeriesUrl, SeriesUrl, new TrangaImage());

        List<ChapterInfo>? chapters = await _extension.GetChapters(manga, ct);

        Assert.NotNull(chapters);
        ChapterInfo chapter = Assert.Single(chapters);
        Assert.Equal("1", chapter.Number);
        Assert.Equal($"{SeriesUrl}c001/", chapter.Url);
        Assert.Equal(DateTimeOffset.Parse("2017-05-09T00:00:00Z"), chapter.ReleaseDate);
    }

    [Fact]
    public async Task EveryPageOfAChapterIsDownloaded()
    {
        MangaInfo manga = new(_extension.Identifier, "Naruto dj - Family Wedding", SeriesUrl, SeriesUrl, new TrangaImage());
        List<ChapterInfo>? chapters = await _extension.GetChapters(manga, ct);
        Assert.NotNull(chapters);

        // The chapter has two pages, each on its own HTML page. GetChapterImages re-encodes every page, so anything
        // that is not an image (for example the CDN's 403 page for a missing Referer) makes it return null.
        List<ChapterImage>? images = await ((IDownloadExtension)_extension).GetChapterImages(chapters.Single(), ct);

        Assert.NotNull(images);
        Assert.Equal(2, images.Count);
        Assert.Equal([0, 1], images.Select(i => i.order));
    }

    [Fact]
    public void ParseIdentifierFromUrl()
    {
        Assert.Equal(SeriesUrl, _extension.ParseIdentifierFromUrl(SeriesUrl));
        // with or without www
        Assert.Equal("https://mangatown.com/manga/naruto_dj_family_wedding/", _extension.ParseIdentifierFromUrl("https://mangatown.com/manga/naruto_dj_family_wedding/"));
        Assert.Null(_extension.ParseIdentifierFromUrl($"{SeriesUrl}c001/"));
        Assert.Null(_extension.ParseIdentifierFromUrl("https://example.com/manga/naruto_dj_family_wedding/"));
    }
}

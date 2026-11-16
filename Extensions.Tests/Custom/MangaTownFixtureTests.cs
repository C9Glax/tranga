using System.Text.RegularExpressions;
using Common.Datatypes;
using Common.Helpers;
using Extensions.Custom;
using Extensions.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Extensions.Tests.Custom;

/// <summary>
/// The MangaTown example definition against frozen, trimmed copies of the real responses
/// (<c>Fixtures/mangatown-*.html</c>). MangaTown is structured very differently from WeebCentral (chapter list on
/// the series page, chapter numbers only in the URL, one HTML page per image), so this shows that the selector
/// engine is not tied to one website.
/// </summary>
public sealed class MangaTownFixtureTests : Common.Tests.TrangaTest, IDisposable
{
    private const string ChapterPath = "/manga/naruto_dj_family_wedding/c001/";

    private readonly FixtureSite _site = new();
    private readonly SelectorDownloadExtension _extension;

    public MangaTownFixtureTests()
    {
        SelectorDownloaderDefinition example = SelectorDownloaderJson.MangaTownExample();
        _extension = new SelectorDownloadExtension(example with
        {
            Id = Guid.NewGuid(),
            BaseUrl = _site.BaseUrl,
            RequestsPerSecond = 20,
            SeriesUrlPattern = $"^{Regex.Escape(_site.BaseUrl)}/manga/(?<id>[^/?#]+)/?$"
        }, new SelectorDownloaderOptions(AllowPrivateHosts: true, FlareSolverrUrl: null));

        _site
            .Html("/search?name=naruto", Fixture("mangatown-search.html"))
            .Html("/manga/vagabond/", Fixture("mangatown-chapters.html"))
            .Html(ChapterPath, Fixture("mangatown-chapter.html", pointImagesToFixtureSite: true))
            .Html($"{ChapterPath}2.html", Fixture("mangatown-page2.html", pointImagesToFixtureSite: true))
            .Map("/store/manga/27207/001.0/compressed/n001.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()))
            .Map("/store/manga/27207/001.0/compressed/n002.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()));
    }

    public void Dispose()
    {
        _extension.Dispose();
        _site.Dispose();
    }

    private string Fixture(string name, bool pointImagesToFixtureSite = false)
    {
        string html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Custom", "Fixtures", name));
        // the real images live on a CDN, the fixture site serves them instead
        return pointImagesToFixtureSite ? html.Replace("//zjcdn.mangahere.org/", $"{_site.BaseUrl}/") : html;
    }

    private static byte[] Jpeg()
    {
        using Image<Rgb24> image = new(4, 4);
        using MemoryStream stream = new();
        image.SaveAsJpeg(stream);
        return stream.ToArray();
    }

    private MangaInfo Series(string path) => new(_extension.Identifier, "Series", $"{_site.BaseUrl}{path}", $"{_site.BaseUrl}{path}", new TrangaImage());

    [Fact]
    public async Task SearchExtractsTheTitleAndNotTheGenreOrLatestChapterLinks()
    {
        StepTestResult result = await _extension.TestSearchAsync("naruto", ct);

        Assert.Null(result.Error);
        Assert.Empty(result.Warnings);
        Assert.Equal(3, result.ItemCount);
        Dictionary<string, string?> first = result.Rows[0];
        Assert.Equal("Naruto dj - Kakashi x Iruka", first["title"]);
        Assert.Equal($"{_site.BaseUrl}/manga/naruto_dj_kakashi_x_iruka/", first["url"]);
        Assert.StartsWith("http://fmcdn.mangahere.com/store/manga/9217/ocover.jpg", first["cover"]);
        Assert.Equal(["Naruto dj - Kakashi x Iruka", "Naruto dj - Daikirai", "Naruto dj - Family Wedding"], result.Rows.Select(r => r["title"]));
    }

    [Fact]
    public async Task ChaptersAreOnTheSeriesPageAndTheirNumberComesFromTheUrlNotTheVolume()
    {
        List<ChapterInfo>? chapters = await _extension.GetChapters(Series("/manga/vagabond/"), ct);

        Assert.NotNull(chapters);
        Assert.Equal(["327", "326", "325"], chapters.Select(c => c.Number));
        Assert.Equal($"{_site.BaseUrl}/manga/vagabond/v33/c327/", chapters[0].Url);
        Assert.All(chapters, c => Assert.Equal(c.Url, c.Identifier));
        // the "Vol 33" span has no class either, the title is the last one of them (and missing when there is only the volume)
        Assert.Equal(["The Man Named Tadaoki", "To be a Samurai", null], chapters.Select(c => c.Title));
        Assert.Equal(DateTimeOffset.Parse("2015-05-25T00:00:00Z"), chapters[0].ReleaseDate);
        Assert.All(chapters, c => Assert.NotNull(c.ReleaseDate));
    }

    [Fact]
    public async Task DecimalChaptersAndRelativeDatesAreHandled()
    {
        _site.Html("/manga/odd/", """
            <ul class="chapter_list">
              <li><a href="/manga/odd/c006.5/">Odd 6.5</a><span class="time">Today</span></li>
              <li><a href="/manga/odd/v02/c006/">Odd 6</a><span class="time">Yesterday</span></li>
              <li><a href="/manga/odd/c001/">Odd 1</a><span class="time">Jan 05,2020</span></li>
            </ul>
            """);

        List<ChapterInfo>? chapters = await _extension.GetChapters(Series("/manga/odd/"), ct);

        Assert.NotNull(chapters);
        Assert.Equal(["6.5", "6", "1"], chapters.Select(c => c.Number));
        // "Today" and "Yesterday" cannot be parsed with a date format: the date is simply unknown
        Assert.Equal([null, null, DateTimeOffset.Parse("2020-01-05T00:00:00Z")], chapters.Select(c => c.ReleaseDate));
    }

    [Fact]
    public async Task ThePagesOfAChapterAreListedFromThePageDropdownWithoutTheFeaturedPage()
    {
        StepTestResult result = await _extension.TestImagesAsync($"{_site.BaseUrl}{ChapterPath}", ct);

        Assert.Null(result.Error);
        Assert.Empty(result.Warnings);
        Assert.Equal(2, result.ItemCount); // the first page is listed once although the page list is on the page twice
        Assert.Equal([$"{_site.BaseUrl}{ChapterPath}", $"{_site.BaseUrl}{ChapterPath}2.html"], result.Rows.Select(r => r["page"]));
        Assert.Equal([$"{_site.BaseUrl}/store/manga/27207/001.0/compressed/n001.jpg", $"{_site.BaseUrl}/store/manga/27207/001.0/compressed/n002.jpg"],
            result.Rows.Select(r => r["image"]));
    }

    [Fact]
    public async Task EveryPageOfAChapterIsDownloadedInOrderWithTheChapterAsReferer()
    {
        string chapterUrl = $"{_site.BaseUrl}{ChapterPath}";

        List<ChapterImage>? images = await ((IDownloadExtension)_extension).GetChapterImages(
            new ChapterInfo(_extension.Identifier, "1", chapterUrl, chapterUrl), ct);

        Assert.NotNull(images);
        Assert.Equal([0, 1], images.Select(i => i.order));
        // MangaTown's CDN answers 403 without a Referer
        Assert.All(_site.Requests.Where(r => r.PathAndQuery.StartsWith("/store/")), r => Assert.Equal(chapterUrl, r.Referer));
        Assert.Equal(1, _site.Requests.Count(r => r.PathAndQuery == ChapterPath)); // the chapter page is not fetched again for its first page
        Assert.DoesNotContain(_site.Requests, r => r.PathAndQuery.EndsWith("featured.html"));
    }

    [Fact]
    public async Task ChaptersWithoutAPageDropdownFallBackToTheImagesOnTheChapterPage()
    {
        _site
            .Html("/manga/single/c001/", $"""<div id="viewer"><img src="{_site.BaseUrl}/store/manga/27207/001.0/compressed/n001.jpg"><img style="display:none" class="reader-main-img"></div>""");
        string chapterUrl = $"{_site.BaseUrl}/manga/single/c001/";

        List<ChapterImage>? images = await _extension.FetchChapterImages(new ChapterInfo(_extension.Identifier, "1", chapterUrl, chapterUrl), ct);

        Assert.NotNull(images);
        Assert.Single(images);
    }

    [Fact]
    public void OnlySeriesPagesOfTheSiteAreRecognised()
    {
        Assert.Equal($"{_site.BaseUrl}/manga/vagabond/", _extension.ParseIdentifierFromUrl($"{_site.BaseUrl}/manga/vagabond/"));
        Assert.Null(_extension.ParseIdentifierFromUrl($"{_site.BaseUrl}/manga/vagabond/v33/c327/"));
        Assert.Null(_extension.ParseIdentifierFromUrl($"{_site.BaseUrl}/search?name=naruto"));
    }

    [Fact]
    public void TheExampleUsesTheRealSiteByDefault()
    {
        SelectorDownloaderDefinition example = SelectorDownloaderJson.MangaTownExample();

        Assert.Equal("https://www.mangatown.com", example.BaseUrl);
        Assert.Matches(example.SeriesUrlPattern!, "https://www.mangatown.com/manga/vagabond/");
        Assert.DoesNotMatch(example.SeriesUrlPattern!, "https://www.mangatown.com/manga/vagabond/v33/c327/");
    }
}

using System.Text.RegularExpressions;
using Common.Datatypes;
using Common.Helpers;
using Extensions.Custom;
using Extensions.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Extensions.Tests.Custom;

/// <summary>
/// The Manga.uno example definition against frozen, trimmed copies of the real API responses
/// (<c>Fixtures/mangauno-*.json</c>). Manga.uno has no HTML to read: its pages are built by JavaScript from a JSON API.
/// So this covers the JSON format end to end: paths in place of selectors, links built from ids (with the series slug
/// that is not part of a chapter), and the chapter URL pattern that leads from a chapter back to the API.
/// </summary>
public sealed class MangaUnoFixtureTests : Common.Tests.TrangaTest, IDisposable
{
    private const string Json = "application/json; charset=utf-8";

    private readonly FixtureSite _site = new();
    private readonly SelectorDownloadExtension _extension;

    public MangaUnoFixtureTests()
    {
        _extension = new SelectorDownloadExtension(Definition(), new SelectorDownloaderOptions(AllowPrivateHosts: true, FlareSolverrUrl: null));

        _site
            .Map("/api/search/advanced?page=1&limit=24&title=one", new FixtureSite.Response(Fixture("mangauno-search.json"), Json))
            .Map("/api/manga/one-piece", new FixtureSite.Response(Fixture("mangauno-manga.json"), Json))
            .Map("/api/chapter/800778", new FixtureSite.Response(Fixture("mangauno-chapter.json"), Json));
        foreach (int page in (int[])[1, 2, 3])
            _site.Map($"/pages/3/800778/{page}.webp", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()));
    }

    /// <summary>The example, pointed at the fixture site (which also stands in for the image CDN).</summary>
    private SelectorDownloaderDefinition Definition()
    {
        SelectorDownloaderDefinition example = SelectorDownloaderJson.MangaUnoExample();
        return example with
        {
            Id = Guid.NewGuid(),
            BaseUrl = _site.BaseUrl,
            RequestsPerSecond = 20,
            SeriesUrlPattern = $"^{Regex.Escape(_site.BaseUrl)}/m/(?<slug>[^/?#]+)",
            Search = example.Search with { Cover = example.Search.Cover with { Replacement = _site.BaseUrl } }
        };
    }

    public void Dispose()
    {
        _extension.Dispose();
        _site.Dispose();
    }

    private string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Custom", "Fixtures", name))
        .Replace("https://xz7.fstr-cdn.com", _site.BaseUrl);

    private static byte[] Jpeg()
    {
        using Image<Rgb24> image = new(4, 4);
        using MemoryStream stream = new();
        image.SaveAsJpeg(stream);
        return stream.ToArray();
    }

    private MangaInfo OnePiece => new(_extension.Identifier, "One Piece", $"{_site.BaseUrl}/m/one-piece", $"{_site.BaseUrl}/m/one-piece", new TrangaImage());

    [Fact]
    public async Task SearchReadsTheApiAndBuildsTheLinksFromTheSlug()
    {
        StepTestResult result = await _extension.TestSearchAsync("one", ct);

        Assert.Null(result.Error);
        Assert.Empty(result.Warnings);
        Assert.Equal(3, result.ItemCount);
        // the English title where there is one ("One Punch-Man" is the plain title of the second)
        Assert.Equal(["One Piece", "One-Punch Man", "Dr. STONE"], result.Rows.Select(r => r["title"]));
        Assert.Equal($"{_site.BaseUrl}/m/one-piece", result.Rows[0]["url"]);
        Assert.Equal($"{_site.BaseUrl}/covers/one-piece.webp", result.Rows[0]["cover"]);
    }

    [Fact]
    public async Task ChaptersGetTheSeriesSlugInTheirLinkAlthoughItIsNotPartOfAChapter()
    {
        List<ChapterInfo>? chapters = await _extension.GetChapters(OnePiece, ct);

        Assert.NotNull(chapters);
        Assert.Equal(["1193", "1182", "1080.1", "0"], chapters.Select(c => c.Number)); // "1193.0000", "1080.1000", ...
        Assert.Equal($"{_site.BaseUrl}/r/one-piece/1159733336", chapters[0].Url);
        Assert.All(chapters, c => Assert.Equal(c.Url, c.Identifier));
        Assert.Equal("Zaza", chapters[1].Title);
        // the newest chapter has no publishing date yet, so the date it was added is used
        Assert.Equal(DateTimeOffset.Parse("2026-09-11T20:31:46Z"), chapters[0].ReleaseDate);
        Assert.Equal(DateTimeOffset.Parse("2026-05-12T00:00:00Z"), chapters[1].ReleaseDate);
        Assert.Contains(_site.Requests, r => r.PathAndQuery == "/api/manga/one-piece");
    }

    [Fact]
    public async Task ThePagesAreReadFromTheApiOfTheChapterInOrder()
    {
        string chapterUrl = $"{_site.BaseUrl}/r/one-piece/800778";

        StepTestResult result = await _extension.TestImagesAsync(chapterUrl, ct);

        Assert.Null(result.Error);
        Assert.Empty(result.Warnings);
        Assert.Equal($"{_site.BaseUrl}/api/chapter/800778", result.RequestUrl);
        Assert.Equal([1, 2, 3], result.Rows.Select(r => int.Parse(Path.GetFileNameWithoutExtension(r["url"]!))));
    }

    [Fact]
    public async Task EveryPageOfAChapterIsDownloadedWithTheChapterAsReferer()
    {
        string chapterUrl = $"{_site.BaseUrl}/r/one-piece/800778";

        List<ChapterImage>? images = await ((IDownloadExtension)_extension).GetChapterImages(
            new ChapterInfo(_extension.Identifier, "1182", chapterUrl, chapterUrl), ct);

        Assert.NotNull(images);
        Assert.Equal([0, 1, 2], images.Select(i => i.order));
        Assert.All(_site.Requests.Where(r => r.PathAndQuery.StartsWith("/pages/")), r => Assert.Equal(chapterUrl, r.Referer));
    }

    [Fact]
    public async Task AChapterUrlThatDoesNotMatchThePatternIsAnErrorAndNotARequest()
    {
        StepTestResult result = await _extension.TestImagesAsync($"{_site.BaseUrl}/m/one-piece", ct);

        Assert.Contains("chapter URL pattern", result.Error);
        Assert.Empty(_site.Requests);
        Assert.Null(await ((IDownloadExtension)_extension).GetChapterImages(
            new ChapterInfo(_extension.Identifier, "1", $"{_site.BaseUrl}/m/one-piece", $"{_site.BaseUrl}/m/one-piece"), ct));
    }

    [Fact]
    public async Task AnHtmlAnswerIsReportedAsNotBeingJson()
    {
        _site.Html("/api/search/advanced?page=1&limit=24&title=shell", "<!doctype html><html><body><div id=\"app\"></div></body></html>");

        StepTestResult result = await _extension.TestSearchAsync("shell", ct);

        Assert.Contains("did not answer with JSON", result.Error);
        Assert.Contains("HTML", result.Error);
        // and for the real thing it is simply "nothing found"
        Assert.Null(await _extension.SearchDownload(new SearchQuery { Title = "shell", Language = "en" }, ct));
    }

    [Fact]
    public async Task AnHtmlSourceThatGetsJsonSuggestsTheJsonFormat()
    {
        SelectorDownloaderDefinition html = Definition() with
        {
            Format = SourceFormat.Html,
            Search = new SearchSpec
            {
                UrlTemplate = "/api/search/advanced?page={page}&limit=24&title={query}", ItemSelector = "div.result",
                Title = new SelectorField { Selector = "a" }, Url = new SelectorField { Selector = "a", Attribute = "href" },
                Cover = new SelectorField { Selector = "img", Attribute = "src" }
            },
            Chapters = new ChaptersSpec { ItemSelector = "li", Url = new SelectorField { Selector = "a", Attribute = "href" } },
            Images = new ImagesSpec { ImageSelector = "img" }
        };
        using SelectorDownloadExtension extension = new(html, new SelectorDownloaderOptions(AllowPrivateHosts: true, FlareSolverrUrl: null));

        StepTestResult result = await extension.TestSearchAsync("one", ct);

        Assert.Null(result.Error);
        Assert.Contains(result.Warnings, w => w.Contains("answered with JSON"));
    }

    [Fact]
    public void OnlySeriesPagesOfTheSiteAreRecognised()
    {
        Assert.Equal($"{_site.BaseUrl}/m/one-piece", _extension.ParseIdentifierFromUrl($"{_site.BaseUrl}/m/one-piece"));
        Assert.Null(_extension.ParseIdentifierFromUrl($"{_site.BaseUrl}/r/one-piece/800778")); // a chapter
        Assert.Null(_extension.ParseIdentifierFromUrl("https://example.com/m/one-piece"));
    }

    [Fact]
    public async Task ApiRequestsAskForJsonAndKeepTheBrowserUserAgent()
    {
        await _extension.TestSearchAsync("one", ct);

        FixtureSite.ReceivedRequest request = Assert.Single(_site.Requests);
        Assert.Equal(SelectorDownloadExtension.DefaultUserAgent, request.UserAgent);
        Assert.StartsWith("application/json", request.Accept);
    }
}

using Common.Datatypes;
using Common.Helpers;
using Extensions.Custom;
using Extensions.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Extensions.Tests.Custom;

/// <summary>Runs the selector engine against a local fixture website.</summary>
public sealed class SelectorDownloadExtensionTests : Common.Tests.TrangaTest, IDisposable
{
    private static readonly SelectorDownloaderOptions AllowLocalhost = new(AllowPrivateHosts: true, FlareSolverrUrl: null);

    private readonly FixtureSite _site = new();
    private readonly List<IDisposable> _disposables = [];

    public void Dispose()
    {
        foreach (IDisposable disposable in _disposables)
            disposable.Dispose();
        _site.Dispose();
    }

    private SelectorDownloaderDefinition Definition() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Fixture",
        BaseUrl = _site.BaseUrl,
        RequestsPerSecond = 20,
        Search = new SearchSpec
        {
            UrlTemplate = "/search?q={query}",
            ItemSelector = "div.result",
            Title = new SelectorField { Selector = "a.title" },
            Url = new SelectorField { Selector = "a.title", Attribute = "href" },
            Cover = new SelectorField { Selector = "img", Attribute = "data-src,src" }
        },
        Chapters = new ChaptersSpec
        {
            ItemSelector = "ul.chapters > li",
            Url = new SelectorField { Selector = "a", Attribute = "href" },
            Number = new SelectorField { Selector = "a", Regex = @"Chapter\s+(\d+(?:\.\d+)?)" },
            Title = new SelectorField { Selector = "span.title" },
            Date = new SelectorField { Selector = "time", Attribute = "datetime" }
        },
        Images = new ImagesSpec { ImageSelector = "div.reader img" }
    };

    private SelectorDownloadExtension Extension(SelectorDownloaderDefinition? definition = null, SelectorDownloaderOptions? options = null)
    {
        SelectorDownloadExtension extension = new(definition ?? Definition(), options ?? AllowLocalhost);
        _disposables.Add(extension);
        return extension;
    }

    private static byte[] Jpeg()
    {
        using Image<Rgb24> image = new(4, 4);
        using MemoryStream stream = new();
        image.SaveAsJpeg(stream);
        return stream.ToArray();
    }

    private FixtureSite WithCovers() => _site
        .Map("/covers/1.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()))
        .Map("/covers/2.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()));

    private static MangaInfo Manga(string identifier) => new(Guid.Empty, "Series", identifier, identifier, new TrangaImage());

    #region Search

    [Fact]
    public async Task SearchReturnsSeriesWithCovers()
    {
        WithCovers().Html("/search?q=one%20piece", """
            <div class="result"><a class="title" href="/series/1">One Piece</a><img data-src="/covers/1.jpg" src="/placeholder.gif"></div>
            <div class="result"><a class="title" href="/series/2">One Piece Party</a><img src="/covers/2.jpg"></div>
            <div class="result"><a class="title" href="/series/1">One Piece (duplicate)</a><img src="/covers/1.jpg"></div>
            <div class="result"><a class="title" href="/series/3">No cover</a></div>
            """);

        List<MangaInfo>? result = await Extension().SearchDownload(new SearchQuery { Title = "one piece", Language = "en" }, ct);

        Assert.NotNull(result);
        Assert.Collection(result,
            first =>
            {
                Assert.Equal("One Piece", first.Title);
                Assert.Equal($"{_site.BaseUrl}/series/1", first.Url);
                Assert.Equal(first.Url, first.Identifier);
                Assert.True(first.Cover.Length > 0);
                Assert.False(first.NSFW);
            },
            second => Assert.Equal("One Piece Party", second.Title));
        // the lazy-loaded cover wins over the placeholder, and the cover download was not the placeholder
        Assert.DoesNotContain(_site.Requests, r => r.PathAndQuery == "/placeholder.gif");
    }

    [Fact]
    public async Task SearchEncodesTheQuery()
    {
        _site.Html("/search?q=a%26b%20%C3%BC", "<div class=\"result\"></div>");

        List<MangaInfo>? result = await Extension().SearchDownload(new SearchQuery { Title = "a&b ü", Language = "en" }, ct);

        Assert.NotNull(result);
        Assert.Contains(_site.Requests, r => r.PathAndQuery == "/search?q=a%26b%20%C3%BC");
    }

    [Fact]
    public async Task SearchInAnotherLanguageFindsNothingWithoutRequestingAnything()
    {
        List<MangaInfo>? result = await Extension().SearchDownload(new SearchQuery { Title = "x", Language = "de" }, ct);

        Assert.NotNull(result);
        Assert.Empty(result);
        Assert.Empty(_site.Requests);
    }

    [Fact]
    public async Task SearchLimitsTheNumberOfResults()
    {
        WithCovers().Html("/search?q=x", string.Concat(Enumerable.Range(1, 25).Select(i =>
            $"<div class=\"result\"><a class=\"title\" href=\"/series/{i}\">S{i}</a><img src=\"/covers/1.jpg\"></div>")));

        List<MangaInfo>? result = await Extension().SearchDownload(new SearchQuery { Title = "x", Language = "en" }, ct);

        Assert.NotNull(result);
        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task SearchReturnsNullWhenTheSiteFails()
    {
        _site.Map("/search?q=x", new FixtureSite.Response("", Status: System.Net.HttpStatusCode.InternalServerError));

        Assert.Null(await Extension().SearchDownload(new SearchQuery { Title = "x", Language = "en" }, ct));
    }

    #endregion

    #region Chapters

    [Fact]
    public async Task ChaptersFollowThePaginationAndSkipEntriesWithoutNumber()
    {
        _site
            .Html("/series/1", """
                <ul class="chapters">
                  <li><a href="/chapter/3">Chapter 3</a><span class="title">Third</span><time datetime="2026-03-01T00:00:00Z"></time></li>
                  <li><a href="/chapter/2">Chapter 2.5</a><time datetime="2026-02-01T10:00:00Z"></time></li>
                </ul><a class="next" href="/series/1?page=2">next</a>
                """)
            .Html("/series/1?page=2", """
                <ul class="chapters">
                  <li><a href="/chapter/1">Chapter 1</a><time datetime="not a date"></time></li>
                  <li><a href="/chapter/extra">Bonus</a></li>
                  <li><a href="/chapter/3">Chapter 3 (again)</a></li>
                </ul>
                """);
        SelectorDownloaderDefinition definition = Definition() with
        {
            Chapters = Definition().Chapters with { NextPage = new SelectorField { Selector = "a.next", Attribute = "href" } }
        };

        List<ChapterInfo>? chapters = await Extension(definition).GetChapters(Manga($"{_site.BaseUrl}/series/1"), ct);

        Assert.NotNull(chapters);
        Assert.Equal(["3", "2.5", "1"], chapters.Select(c => c.Number));
        Assert.Equal([$"{_site.BaseUrl}/chapter/3", $"{_site.BaseUrl}/chapter/2", $"{_site.BaseUrl}/chapter/1"], chapters.Select(c => c.Url));
        Assert.Equal(chapters.Select(c => c.Url), chapters.Select(c => c.Identifier));
        Assert.Equal("Third", chapters[0].Title);
        Assert.Equal(DateTimeOffset.Parse("2026-03-01T00:00:00Z"), chapters[0].ReleaseDate);
        Assert.Null(chapters[2].ReleaseDate); // unparsable date -> none
    }

    [Fact]
    public async Task ChapterPaginationStopsAtLoops()
    {
        _site.Html("/series/1", """
            <ul class="chapters"><li><a href="/chapter/1">Chapter 1</a></li></ul><a class="next" href="/series/1">again</a>
            """);
        SelectorDownloaderDefinition definition = Definition() with
        {
            Chapters = Definition().Chapters with { NextPage = new SelectorField { Selector = "a.next", Attribute = "href" } }
        };

        List<ChapterInfo>? chapters = await Extension(definition).GetChapters(Manga($"{_site.BaseUrl}/series/1"), ct);

        Assert.NotNull(chapters);
        Assert.Single(chapters);
        Assert.Single(_site.Requests, r => r.PathAndQuery == "/series/1");
    }

    [Fact]
    public async Task ChapterListCanLiveAtAnotherUrlThanTheSeriesPage()
    {
        _site.Html("/api/42/chapters", """<ul class="chapters"><li><a href="/chapter/9">Chapter 9</a></li></ul>""");
        SelectorDownloaderDefinition definition = Definition() with
        {
            SeriesUrlPattern = $"^{System.Text.RegularExpressions.Regex.Escape(_site.BaseUrl)}/series/(?<id>\\d+)",
            Chapters = Definition().Chapters with { UrlTemplate = "/api/{id}/chapters" }
        };

        List<ChapterInfo>? chapters = await Extension(definition).GetChapters(Manga($"{_site.BaseUrl}/series/42/some-slug"), ct);

        Assert.NotNull(chapters);
        Assert.Equal("9", Assert.Single(chapters).Number);
    }

    [Fact]
    public async Task ChaptersOfASeriesThatDoesNotMatchThePatternAreNull()
    {
        SelectorDownloaderDefinition definition = Definition() with
        {
            SeriesUrlPattern = $"^{System.Text.RegularExpressions.Regex.Escape(_site.BaseUrl)}/series/(?<id>\\d+)"
        };

        Assert.Null(await Extension(definition).GetChapters(Manga($"{_site.BaseUrl}/other/42"), ct));
    }

    [Fact]
    public async Task ChaptersAreNullWhenThePageIsMissing() =>
        Assert.Null(await Extension().GetChapters(Manga($"{_site.BaseUrl}/series/404"), ct));

    #endregion

    #region Images

    [Fact]
    public async Task ImagesAreDownloadedInOrderWithRefererAndCustomHeaders()
    {
        string chapterUrl = $"{_site.BaseUrl}/chapter/1";
        _site
            .Html("/chapter/1", """
                <div class="reader"><img data-src="/img/1.jpg"><img src="/img/2.jpg"><img src="/img/1.jpg"><img src="/img/3.jpg"></div>
                """)
            .Map("/img/1.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()))
            .Map("/img/2.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()))
            .Map("/img/3.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()));
        SelectorDownloaderDefinition definition = Definition() with
        {
            Headers = new Dictionary<string, string> { ["Cookie"] = "session=abc", ["User-Agent"] = "CustomAgent/1.0" }
        };
        SelectorDownloadExtension extension = Extension(definition);

        List<ChapterImage>? images = await ((IDownloadExtension)extension).GetChapterImages(
            new ChapterInfo(extension.Identifier, "1", chapterUrl, chapterUrl), ct);

        Assert.NotNull(images);
        Assert.Equal([0, 1, 2], images.Select(i => i.order)); // the duplicate was dropped
        Assert.All(images, i => Assert.Equal(chapterUrl, i.chapterIdentifier));
        FixtureSite.ReceivedRequest image = Assert.Single(_site.Requests, r => r.PathAndQuery == "/img/2.jpg");
        Assert.Equal(chapterUrl, image.Referer);
        Assert.Equal("session=abc", image.Cookie);
        Assert.Equal("CustomAgent/1.0", image.UserAgent);
    }

    [Fact]
    public async Task ImagePageUrlCanBeDerivedFromTheChapterUrl()
    {
        string chapterUrl = $"{_site.BaseUrl}/chapter/1";
        _site
            .Html("/chapter/1/images?long=1", """<div class="reader"><img src="/img/1.jpg"></div>""")
            .Map("/img/1.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()));
        SelectorDownloaderDefinition definition = Definition() with { Images = Definition().Images with { UrlTemplate = "{url}/images?long=1" } };
        SelectorDownloadExtension extension = Extension(definition);

        List<ChapterImage>? images = await ((IDownloadExtension)extension).GetChapterImages(new ChapterInfo(extension.Identifier, "1", chapterUrl, chapterUrl), ct);

        Assert.NotNull(images);
        Assert.Single(images);
    }

    [Fact]
    public async Task AChapterWithAMissingPageFails()
    {
        string chapterUrl = $"{_site.BaseUrl}/chapter/1";
        _site
            .Html("/chapter/1", """<div class="reader"><img src="/img/1.jpg"><img src="/img/gone.jpg"></div>""")
            .Map("/img/1.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()));
        SelectorDownloadExtension extension = Extension();

        Assert.Null(await extension.FetchChapterImages(new ChapterInfo(extension.Identifier, "1", chapterUrl, chapterUrl), ct));
    }

    private SelectorDownloaderDefinition WithPageList() => Definition() with
    {
        Images = new ImagesSpec
        {
            PageSelector = new SelectorField { Selector = "select.pages option:not(.skip)", Attribute = "value" },
            ImageSelector = "div.reader img", Attribute = "src"
        }
    };

    private FixtureSite OnePagePerImageChapter(string imageForSecondPage = "/img/2.jpg")
    {
        const string pageList = """
            <select class="pages">
              <option value="/chapter/1" selected>1</option>
              <option value="/chapter/1/2.html">2</option>
              <option value="/chapter/1/3.html">3</option>
              <option class="skip" value="/chapter/1/featured.html">Featured</option>
            </select>
            """;
        return _site
            .Html("/chapter/1", pageList + """<div class="reader"><img src="/img/1.jpg"></div>""")
            .Html("/chapter/1/2.html", pageList + $"""<div class="reader"><img src="{imageForSecondPage}"></div>""")
            .Html("/chapter/1/3.html", pageList + """<div class="reader"><img src="/img/3.jpg"></div>""")
            .Map("/img/1.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()))
            .Map("/img/2.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()))
            .Map("/img/3.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()));
    }

    [Fact]
    public async Task ImagesCanBeSpreadOverOneHtmlPagePerImage()
    {
        string chapterUrl = $"{_site.BaseUrl}/chapter/1";
        OnePagePerImageChapter();
        SelectorDownloadExtension extension = Extension(WithPageList());

        List<ChapterImage>? images = await extension.FetchChapterImages(new ChapterInfo(extension.Identifier, "1", chapterUrl, chapterUrl), ct);

        Assert.NotNull(images);
        Assert.Equal([0, 1, 2], images.Select(i => i.order));
        // in page order, the skipped "featured" option was not followed, and the chapter page (first option) was not requested twice
        Assert.Equal(["/chapter/1", "/chapter/1/2.html", "/chapter/1/3.html"],
            _site.Requests.Select(r => r.PathAndQuery).Where(p => p.StartsWith("/chapter")));
        Assert.Equal(["/img/1.jpg", "/img/2.jpg", "/img/3.jpg"], _site.Requests.Select(r => r.PathAndQuery).Where(p => p.StartsWith("/img")));
        Assert.All(_site.Requests.Where(r => r.PathAndQuery.StartsWith("/img")), r => Assert.Equal(chapterUrl, r.Referer));
    }

    [Fact]
    public async Task AChapterWhoseOnePageFailsToLoadFails()
    {
        string chapterUrl = $"{_site.BaseUrl}/chapter/1";
        OnePagePerImageChapter();
        _site.Map("/chapter/1/3.html", new FixtureSite.Response("", Status: System.Net.HttpStatusCode.InternalServerError));
        SelectorDownloadExtension extension = Extension(WithPageList());

        Assert.Null(await extension.FetchChapterImages(new ChapterInfo(extension.Identifier, "1", chapterUrl, chapterUrl), ct));
    }

    [Fact]
    public async Task AChapterWithAPageWithoutAnImageFails()
    {
        string chapterUrl = $"{_site.BaseUrl}/chapter/1";
        _site
            .Html("/chapter/1", """<select class="pages"><option value="/chapter/1/2.html">2</option></select><div class="reader"><img src="/img/1.jpg"></div>""")
            .Html("/chapter/1/2.html", "<div class=\"reader\"></div>")
            .Map("/img/1.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()));
        SelectorDownloadExtension extension = Extension(WithPageList());

        Assert.Null(await extension.FetchChapterImages(new ChapterInfo(extension.Identifier, "1", chapterUrl, chapterUrl), ct));
    }

    [Fact]
    public async Task WithoutPageLinksTheImagesOfTheChapterPageAreUsed()
    {
        string chapterUrl = $"{_site.BaseUrl}/chapter/1";
        _site
            .Html("/chapter/1", """<div class="reader"><img src="/img/1.jpg"><img src="/img/2.jpg"></div>""")
            .Map("/img/1.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()))
            .Map("/img/2.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()));
        SelectorDownloadExtension extension = Extension(WithPageList());

        List<ChapterImage>? images = await extension.FetchChapterImages(new ChapterInfo(extension.Identifier, "1", chapterUrl, chapterUrl), ct);

        Assert.NotNull(images);
        Assert.Equal(2, images.Count);
    }

    [Fact]
    public async Task TheTestPanelListsThePagesAndLooksIntoTheFirstOnes()
    {
        OnePagePerImageChapter();
        SelectorDownloadExtension extension = Extension(WithPageList());

        StepTestResult result = await extension.TestImagesAsync($"{_site.BaseUrl}/chapter/1", ct);

        Assert.Null(result.Error);
        Assert.Empty(result.Warnings);
        Assert.Equal(3, result.ItemCount);
        Assert.Equal([$"{_site.BaseUrl}/chapter/1", $"{_site.BaseUrl}/chapter/1/2.html", $"{_site.BaseUrl}/chapter/1/3.html"], result.Rows.Select(r => r["page"]));
        Assert.Equal([$"{_site.BaseUrl}/img/1.jpg", $"{_site.BaseUrl}/img/2.jpg", $"{_site.BaseUrl}/img/3.jpg"], result.Rows.Select(r => r["image"]));
    }

    [Fact]
    public async Task TheTestPanelWarnsWhenAPageHasNoImage()
    {
        _site
            .Html("/chapter/1", """<select class="pages"><option value="/chapter/1/2.html">2</option></select>""")
            .Html("/chapter/1/2.html", "<p>nothing here</p>");
        SelectorDownloadExtension extension = Extension(WithPageList());

        StepTestResult result = await extension.TestImagesAsync($"{_site.BaseUrl}/chapter/1", ct);

        Assert.Null(result.Error);
        Assert.Contains(result.Warnings, w => w.Contains("no image") && w.Contains("/chapter/1/2.html"));
    }

    [Fact]
    public async Task AChapterWithoutAPageListIsTestedLikeBefore()
    {
        _site.Html("/chapter/1", """<div class="reader"><img src="/img/1.jpg"></div>""");
        SelectorDownloadExtension extension = Extension();

        StepTestResult result = await extension.TestImagesAsync($"{_site.BaseUrl}/chapter/1", ct);

        Assert.Equal(1, result.ItemCount);
        Assert.Equal($"{_site.BaseUrl}/img/1.jpg", result.Rows[0]["url"]);
    }

    [Fact]
    public async Task AChapterWithoutImagesIsNull()
    {
        string chapterUrl = $"{_site.BaseUrl}/chapter/1";
        _site.Html("/chapter/1", "<div class=\"reader\"></div>");
        SelectorDownloadExtension extension = Extension();

        Assert.Null(await extension.FetchChapterImages(new ChapterInfo(extension.Identifier, "1", chapterUrl, chapterUrl), ct));
    }

    #endregion

    #region Requests

    [Fact]
    public async Task RequestsLookLikeABrowserUnlessTheUserAgentIsOverridden()
    {
        _site.Html("/search?q=x", "");

        await Extension().SearchDownload(new SearchQuery { Title = "x", Language = "en" }, ct);

        Assert.Equal(SelectorDownloadExtension.DefaultUserAgent, Assert.Single(_site.Requests).UserAgent);
    }

    [Fact]
    public async Task PrivateHostsAreRefusedUnlessAllowed()
    {
        _site.Html("/search?q=x", """<div class="result"></div>""");
        SelectorDownloadExtension strict = Extension(options: new SelectorDownloaderOptions(AllowPrivateHosts: false, FlareSolverrUrl: null));

        Assert.Null(await strict.SearchDownload(new SearchQuery { Title = "x", Language = "en" }, ct));
        Assert.Empty(_site.Requests);

        StepTestResult test = await strict.TestSearchAsync("x", ct);
        Assert.NotNull(test.Error);
        Assert.Contains("ALLOW_PRIVATE_SOURCE_HOSTS", test.Error);
    }

    [Fact]
    public void InvalidDefinitionsAreRejected() =>
        Assert.Throws<ArgumentException>(() => new SelectorDownloadExtension(Definition() with { Name = "" }, AllowLocalhost));

    [Fact]
    public void ExposesTheDefinitionAsExtension()
    {
        SelectorDownloaderDefinition definition = Definition() with { Languages = ["en", "pt-BR"], IconUrl = "https://example.com/i.png" };
        SelectorDownloadExtension extension = Extension(definition);

        Assert.Equal(definition.Id, extension.Identifier);
        Assert.Equal("Fixture", extension.Name);
        Assert.Equal(_site.BaseUrl, extension.BaseUrl);
        Assert.Equal("https://example.com/i.png", extension.IconUrl);
        Assert.Equal(["en", "pt-BR"], extension.SupportedLanguages.Select(l => l.Name));
        Assert.Equal($"{_site.BaseUrl}/favicon.ico", Extension().IconUrl);
    }

    [Theory]
    [InlineData("https://x.test/series/7", "https://x.test", "https://x.test/series/7")]
    [InlineData("https://www.x.test/series/7?a=1#f", "https://x.test", "https://www.x.test/series/7?a=1")]
    [InlineData("https://other.test/series/7", "https://x.test", null)]
    [InlineData("ftp://x.test/series/7", "https://x.test", null)]
    [InlineData("nonsense", "https://x.test", null)]
    public void ParseIdentifierFromUrl(string url, string baseUrl, string? expected)
    {
        using SelectorDownloadExtension extension = new(Definition() with { BaseUrl = baseUrl }, AllowLocalhost);
        Assert.Equal(expected, extension.ParseIdentifierFromUrl(url));
    }

    [Theory]
    [InlineData("Chapter 12", "12")]
    [InlineData("Ch. 012.5: Title", "12.5")]
    [InlineData("Vol 2 Chapter 7", "2")] // the first number wins; use a regex on the field to be more specific
    [InlineData("1,198", "1198")]
    [InlineData("007", "7")]
    [InlineData("Bonus", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ParseChapterNumber(string? text, string? expected) => Assert.Equal(expected, SelectorDownloadExtension.ParseChapterNumber(text));

    [Theory]
    [InlineData("2026-01-02T03:04:05Z", null, "2026-01-02T03:04:05+00:00")]
    [InlineData("2026-01-02", null, "2026-01-02T00:00:00+00:00")]
    [InlineData("02/01/2026", "dd/MM/yyyy", "2026-01-02T00:00:00+00:00")]
    [InlineData("02/01/2026", "yyyy-MM-dd", null)]
    [InlineData("not a date", null, null)]
    [InlineData(null, null, null)]
    public void ParseDate(string? text, string? format, string? expected) =>
        Assert.Equal(expected is null ? null : DateTimeOffset.Parse(expected), SelectorDownloadExtension.ParseDate(text, format));

    #endregion

    #region Test steps

    [Fact]
    public async Task TestSearchShowsExtractedRowsAndWarnsAboutSkippedItems()
    {
        _site.Html("/search?q=x", """
            <div class="result"><a class="title" href="/series/1">One</a><img src="/covers/1.jpg"></div>
            <div class="result"><a class="title" href="/series/2">Two (no cover)</a></div>
            """);

        StepTestResult result = await Extension().TestSearchAsync("x", ct);

        Assert.Null(result.Error);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal($"{_site.BaseUrl}/search?q=x", result.RequestUrl);
        Assert.Equal(1, result.ItemCount);
        Assert.Equal("One", Assert.Single(result.Rows)["title"]);
        Assert.Equal($"{_site.BaseUrl}/covers/1.jpg", result.Rows[0]["cover"]);
        Assert.Contains(result.Warnings, w => w.Contains("1 of 2") && w.Contains("cover"));
    }

    [Fact]
    public async Task ATrailingSlashOfTheBaseUrlMakesNoDifference()
    {
        _site.Html("/search?q=x", """<div class="result"><a class="title" href="/series/1">One</a><img src="/covers/1.jpg"></div>""");
        SelectorDownloadExtension extension = Extension(Definition() with { BaseUrl = $"  {_site.BaseUrl}/  " });

        StepTestResult result = await extension.TestSearchAsync("x", ct);

        Assert.Equal(_site.BaseUrl, extension.BaseUrl);
        Assert.Equal($"{_site.BaseUrl}/search?q=x", result.RequestUrl);
        Assert.Equal(1, result.ItemCount);
        Assert.Equal($"{_site.BaseUrl}/series/1", result.Rows[0]["url"]);
    }

    [Fact]
    public async Task TestWarnsWhenTheItemSelectorMatchesNothing()
    {
        _site.Html("/search?q=x", "<p>rendered by javascript</p>");

        StepTestResult result = await Extension().TestSearchAsync("x", ct);

        Assert.Null(result.Error);
        Assert.Equal(0, result.ItemCount);
        Assert.Contains(result.Warnings, w => w.Contains("matched no elements"));
    }

    [Fact]
    public async Task TestReportsHttpErrorsAndHintsAtFlareSolverrForCloudflareStatuses()
    {
        _site.Map("/search?q=x", new FixtureSite.Response("blocked", Status: System.Net.HttpStatusCode.Forbidden));

        StepTestResult result = await Extension().TestSearchAsync("x", ct);

        Assert.Equal(403, result.StatusCode);
        Assert.NotNull(result.Error);
        Assert.Contains("403", result.Error);
        Assert.Contains("FlareSolverr", result.Error);
    }

    [Fact]
    public async Task TestWarnsWhenFlareSolverrIsRequestedButNotConfigured()
    {
        _site.Html("/search?q=x", "<p></p>");

        StepTestResult result = await Extension(Definition() with { UseFlareSolverr = true }).TestSearchAsync("x", ct);

        Assert.Contains(result.Warnings, w => w.Contains("FlareSolverr") && w.Contains("no FlareSolverr instance"));
    }

    [Fact]
    public async Task TestChaptersReportsNumbersTitlesAndDates()
    {
        _site.Html("/series/1", """
            <ul class="chapters"><li><a href="/chapter/5">Chapter 5</a><span class="title">Five</span><time datetime="2026-05-05T00:00:00Z"></time></li></ul>
            """);

        StepTestResult result = await Extension().TestChaptersAsync($"{_site.BaseUrl}/series/1", ct);

        Assert.Null(result.Error);
        Dictionary<string, string?> row = Assert.Single(result.Rows);
        Assert.Equal("5", row["number"]);
        Assert.Equal("Five", row["title"]);
        Assert.Equal($"{_site.BaseUrl}/chapter/5", row["url"]);
        Assert.NotNull(row["date"]);
    }

    [Fact]
    public async Task TestChaptersRejectsForeignUrls()
    {
        StepTestResult result = await Extension().TestChaptersAsync("https://elsewhere.test/series/1", ct);

        Assert.NotNull(result.Error);
        Assert.Empty(_site.Requests);
    }

    [Fact]
    public async Task TestImagesListsUrlsWithoutDownloadingThem()
    {
        _site.Html("/chapter/1", """<div class="reader"><img src="/img/1.jpg"><img src="/img/2.jpg"></div>""");

        StepTestResult result = await Extension().TestImagesAsync($"{_site.BaseUrl}/chapter/1", ct);

        Assert.Null(result.Error);
        Assert.Equal(2, result.ItemCount);
        Assert.Equal([$"{_site.BaseUrl}/img/1.jpg", $"{_site.BaseUrl}/img/2.jpg"], result.Rows.Select(r => r["url"]));
        Assert.DoesNotContain(_site.Requests, r => r.PathAndQuery.StartsWith("/img/"));
    }

    [Fact]
    public async Task TestStepWorksWhileOtherSectionsAreStillEmpty()
    {
        _site.Html("/search?q=x", """<div class="result"><a class="title" href="/series/1">One</a><img src="/covers/1.jpg"></div>""");
        SelectorDownloaderDefinition onlySearch = Definition() with { Chapters = new ChaptersSpec(), Images = new ImagesSpec() };

        using SelectorDownloadExtension extension = SelectorDownloadExtension.CreateForTest(onlySearch, SelectorDownloaderStep.Search, AllowLocalhost);
        StepTestResult result = await extension.TestSearchAsync("x", ct);

        Assert.Null(result.Error);
        Assert.Equal(1, result.ItemCount);
        Assert.Throws<ArgumentException>(() => SelectorDownloadExtension.CreateForTest(onlySearch, SelectorDownloaderStep.Chapters, AllowLocalhost));
    }

    #endregion
}

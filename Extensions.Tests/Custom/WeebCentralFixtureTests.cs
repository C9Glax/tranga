using System.Text.RegularExpressions;
using Common.Helpers;
using Extensions.Custom;
using Extensions.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Extensions.Tests.Custom;

/// <summary>
/// The WeebCentral example definition against frozen, trimmed copies of the real responses
/// (<c>Fixtures/weebcentral-*.html</c>), so the selectors keep being checked without network access.
/// </summary>
public sealed class WeebCentralFixtureTests : Common.Tests.TrangaTest, IDisposable
{
    private const string SeriesId = "01J76XY7E9FNDZ1DBBM6PBJPFK";

    private readonly FixtureSite _site = new();
    private readonly SelectorDownloadExtension _extension;

    public WeebCentralFixtureTests()
    {
        SelectorDownloaderDefinition example = SelectorDownloaderJson.WeebCentralExample();
        _extension = new SelectorDownloadExtension(example with
        {
            Id = Guid.NewGuid(),
            BaseUrl = _site.BaseUrl,
            RequestsPerSecond = 20,
            SeriesUrlPattern = $"^{Regex.Escape(_site.BaseUrl)}/series/(?<id>[^/?#]+)"
        }, new SelectorDownloaderOptions(AllowPrivateHosts: true, FlareSolverrUrl: null));

        _site
            .Html("/search/data?text=one%20piece&limit=32&offset=0&display_mode=Full%20Display", Fixture("weebcentral-search.html"))
            .Html($"/series/{SeriesId}/full-chapter-list", Fixture("weebcentral-chapters.html"))
            .Html("/chapters/01M6RHBP1Y3A03BK0TZMBJ2YZN/images?is_prev=False&reading_style=long_strip", Fixture("weebcentral-images.html"));
    }

    public void Dispose()
    {
        _extension.Dispose();
        _site.Dispose();
    }

    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Custom", "Fixtures", name));

    [Fact]
    public async Task SearchExtractsTitleUrlAndNormalCover()
    {
        StepTestResult result = await _extension.TestSearchAsync("one piece", ct);

        Assert.Null(result.Error);
        Assert.Empty(result.Warnings);
        Assert.Equal(2, result.ItemCount);
        Dictionary<string, string?> first = result.Rows[0];
        Assert.Equal("One Piece", first["title"]);
        Assert.Equal("https://weebcentral.com/series/01J76XY7E9FNDZ1DBBM6PBJPFK/One-Piece", first["url"]);
        // the site offers a "normal" and a "small" cover; the first <source> is the normal one
        Assert.Equal("https://temp.compsci88.com/cover/normal/01J76XY7E9FNDZ1DBBM6PBJPFK.webp", first["cover"]);
        Assert.DoesNotContain("/small/", first["cover"]);
        Assert.Equal("One Piece (Color)", result.Rows[1]["title"]);
    }

    [Fact]
    public async Task ChaptersComeFromTheFullChapterListPageWithRelativeLinks()
    {
        MangaInfo manga = new(_extension.Identifier, "One Piece", $"{_site.BaseUrl}/series/{SeriesId}/One-Piece",
            $"{_site.BaseUrl}/series/{SeriesId}/One-Piece", new TrangaImage());

        List<ChapterInfo>? chapters = await _extension.GetChapters(manga, ct);

        Assert.NotNull(chapters);
        Assert.Equal(5, chapters.Count);
        ChapterInfo newest = chapters[0];
        Assert.Equal("1198", newest.Number);
        Assert.Equal($"{_site.BaseUrl}/chapters/01M6RHBP1Y3A03BK0TZMBJ2YZN", newest.Url);
        Assert.Equal(DateTimeOffset.Parse("2026-11-06T13:39:21.278Z"), newest.ReleaseDate);
        Assert.Equal(["1198", "1197", "1196", "1195", "1194"], chapters.Select(c => c.Number));
        Assert.All(chapters, c => Assert.NotNull(c.ReleaseDate));
    }

    [Fact]
    public async Task ChapterNamesDoNotHaveToContainTheWordChapter()
    {
        // Found with the real site: the Dr. STONE chapters are called "Z= 232.8", not "Chapter 232.8"
        _site.Html("/series/NAMES/full-chapter-list", """
            <div x-data="{}"><a href="/chapters/A"><span class="flex"><span>Z= 232.8</span></span><time datetime="2026-01-01T00:00:00Z"></time></a></div>
            <div x-data="{}"><a href="/chapters/B"><span class="flex"><span>Chapter 12</span></span><time datetime="2026-01-02T00:00:00Z"></time></a></div>
            <div x-data="{}"><a href="/chapters/C"><span class="flex"><span>Season 2 Chapter 5</span></span><time datetime="2026-01-03T00:00:00Z"></time></a></div>
            <div x-data="{}"><a href="/chapters/D"><span class="flex"><span>Extra</span></span><time datetime="2026-01-04T00:00:00Z"></time></a></div>
            """);
        string series = $"{_site.BaseUrl}/series/NAMES/Names";

        List<ChapterInfo>? chapters = await _extension.GetChapters(new MangaInfo(_extension.Identifier, "Names", series, series, new TrangaImage()), ct);

        Assert.NotNull(chapters);
        Assert.Equal(["232.8", "12", "5"], chapters.Select(c => c.Number)); // "Extra" has no number
    }

    [Fact]
    public async Task ImagesAreFoundInTheReaderSection()
    {
        StepTestResult result = await _extension.TestImagesAsync($"{_site.BaseUrl}/chapters/01M6RHBP1Y3A03BK0TZMBJ2YZN", ct);

        Assert.Null(result.Error);
        Assert.Equal(4, result.ItemCount);
        Assert.Equal("https://scans-hot.planeptune.us/manga/One-Piece/1198-001.png", result.Rows[0]["url"]);
        Assert.All(result.Rows, r => Assert.StartsWith("https://", r["url"]));
    }

    [Fact]
    public async Task CoverOfAPastedSeriesUrlIsThePreviewImageOfItsPage()
    {
        // Found with the real site: a series page names its cover for link previews (og:image), a pasted URL has no
        // search result to take it from.
        const string pastedSeriesId = "01JGRW9YR8AYQFAMVQ416AY8GX";
        string seriesUrl = $"{_site.BaseUrl}/series/{pastedSeriesId}/majutsu-wo-kiwamete";
        _site
            .Html($"/series/{pastedSeriesId}/majutsu-wo-kiwamete", Fixture("weebcentral-series.html").Replace("https://temp.compsci88.com", _site.BaseUrl))
            .Map($"/cover/fallback/{pastedSeriesId}.jpg", new FixtureSite.Response("", "image/jpeg", Bytes: Jpeg()));
        MangaInfo manga = new(_extension.Identifier, "", seriesUrl, _extension.ParseIdentifierFromUrl(seriesUrl)!, new TrangaImage());

        TrangaImage? cover = await _extension.GetCover(manga, ct);

        Assert.NotNull(cover);
        Assert.True(cover.Length > 0);
        Assert.Equal(Jpeg().Length, cover.Length);
    }

    private static byte[] Jpeg()
    {
        using Image<Rgb24> image = new(4, 4);
        using MemoryStream stream = new();
        image.SaveAsJpeg(stream);
        return stream.ToArray();
    }

    [Fact]
    public void OnlySeriesPagesOfTheSiteAreRecognised()
    {
        Assert.Equal($"{_site.BaseUrl}/series/{SeriesId}/One-Piece", _extension.ParseIdentifierFromUrl($"{_site.BaseUrl}/series/{SeriesId}/One-Piece"));
        Assert.Null(_extension.ParseIdentifierFromUrl($"{_site.BaseUrl}/chapters/01M6RHBP1Y3A03BK0TZMBJ2YZN"));
    }
}

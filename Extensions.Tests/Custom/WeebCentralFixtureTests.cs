using System.Text.RegularExpressions;
using Common.Helpers;
using Extensions.Custom;
using Extensions.Data;

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
    public async Task ImagesAreFoundInTheReaderSection()
    {
        StepTestResult result = await _extension.TestImagesAsync($"{_site.BaseUrl}/chapters/01M6RHBP1Y3A03BK0TZMBJ2YZN", ct);

        Assert.Null(result.Error);
        Assert.Equal(4, result.ItemCount);
        Assert.Equal("https://scans-hot.planeptune.us/manga/One-Piece/1198-001.png", result.Rows[0]["url"]);
        Assert.All(result.Rows, r => Assert.StartsWith("https://", r["url"]));
    }

    [Fact]
    public void OnlySeriesPagesOfTheSiteAreRecognised()
    {
        Assert.Equal($"{_site.BaseUrl}/series/{SeriesId}/One-Piece", _extension.ParseIdentifierFromUrl($"{_site.BaseUrl}/series/{SeriesId}/One-Piece"));
        Assert.Null(_extension.ParseIdentifierFromUrl($"{_site.BaseUrl}/chapters/01M6RHBP1Y3A03BK0TZMBJ2YZN"));
    }
}

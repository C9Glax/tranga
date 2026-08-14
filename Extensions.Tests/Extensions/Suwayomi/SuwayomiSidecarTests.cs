using Common.Datatypes;
using Common.Settings;
using Extensions.Data;
using Extensions.Extensions.Suwayomi;

namespace Extensions.Tests.Extensions.Suwayomi;

/// <summary>
/// Live tests against a running Suwayomi sidecar. Unlike the other extension tests in this project the dependency is a
/// container the developer has to bring up rather than a public site, so each test probes the sidecar and skips when it
/// is not answering.
/// </summary>
public sealed class SuwayomiSidecarTests : Common.Tests.TrangaTest
{
    /// <returns>The sidecar status, once it is known to be reachable.</returns>
    private async Task<SuwayomiStatus> SkipUnlessReachable()
    {
        SuwayomiStatus status = await SuwayomiExtensionManager.GetStatusAsync(ct);
        Assert.SkipUnless(status.Reachable,
            $"No Suwayomi sidecar answering at {Common.Settings.EnvVars.SuwayomiUrl}; start one to exercise these tests.");
        return status;
    }

    [Fact]
    public async Task SidecarIsReachable()
    {
        SuwayomiStatus status = await SkipUnlessReachable();
        Assert.False(string.IsNullOrEmpty(status.ServerVersion));
    }

    [Fact]
    public async Task ExtensionCatalogueIsPopulated()
    {
        await SkipUnlessReachable();

        // Requires the keiyoushi store to be configured, which the AppHost does via EXTENSION_STORES.
        SuwayomiExtensionInfo[]? extensions = await SuwayomiExtensionManager.GetExtensionsAsync(refresh: true, ct);
        Assert.NotNull(extensions);
        Assert.NotEmpty(extensions);
    }

    [Fact]
    public async Task InstalledSourcesBecomeDownloadExtensions()
    {
        await SkipUnlessReachable();

        SuwayomiSourceInfo[]? sources = await SuwayomiExtensionManager.GetSourcesAsync(ct);
        Assert.NotNull(sources);
        Assert.SkipWhen(sources.Length == 0, "No Suwayomi extensions are installed on the sidecar.");

        int count = await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(ct);
        Assert.Equal(sources.Length, count);

        // Every installed source must resolve back to a registered extension under its derived identifier, since that
        // identifier is what download links are persisted against.
        foreach (SuwayomiSourceInfo source in sources)
        {
            Assert.Equal(SuwayomiSource.IdentifierFor(source.SourceId), source.ExtensionId);
            Assert.NotNull(DownloadExtensionsCollection.GetExtension(source.ExtensionId));
        }
    }

    [Fact]
    public async Task InstalledSourceSearchesAndDownloads()
    {
        await SkipUnlessReachable();

        await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(ct);

        // The built-in "Local source" has no homeUrl and serves files from the sidecar's own disk, so it cannot stand
        // in for a real remote source here.
        SuwayomiSourceInfo[] sources = await SuwayomiExtensionManager.GetSourcesAsync(ct) ?? [];
        SuwayomiSourceInfo[] usable = [.. sources.Where(s =>
            !string.IsNullOrEmpty(s.HomeUrl) && s.ContentWarning is not SuwayomiContentWarning.Nsfw)];

        // Prefer the configured download language. Multi-language extensions register one source per language, and
        // most of those (MangaDex has ~50) carry almost nothing translated, which would make this test skip.
        string downloadLanguage = Settings.DownloadLanguage.TwoLetterISOLanguageName;
        SuwayomiSourceInfo? candidate =
            usable.FirstOrDefault(s => s.Lang.StartsWith(downloadLanguage, StringComparison.OrdinalIgnoreCase))
            ?? usable.FirstOrDefault();
        Assert.SkipWhen(candidate is null, "No remote Suwayomi source is installed on the sidecar.");

        IDownloadExtension extension = Assert.IsType<SuwayomiSource>(DownloadExtensionsCollection.GetExtension(candidate!.ExtensionId));

        // An empty query is how Tachiyomi sources are asked for their default listing, which keeps this test
        // independent of whichever source happens to be installed.
        List<MangaInfo>? searchResult = await extension.SearchDownload(new SearchQuery { Title = string.Empty }, ct);
        Assert.NotNull(searchResult);
        Assert.SkipWhen(searchResult.Count == 0, $"{candidate.Name} returned no manga for an empty query.");

        MangaInfo manga = searchResult[0];
        Assert.False(string.IsNullOrEmpty(manga.Title));
        Assert.True(manga.Cover.Length > 0);

        // Whichever source happens to be installed may legitimately carry entries with no chapters — a MangaDex
        // language variant with nothing translated yet, for instance — so walk the results until one has some.
        // GetChapters must still answer with an empty list rather than null for those, never conflating "none" with
        // "failed"; that is asserted on every candidate along the way.
        List<ChapterInfo>? chapters = null;
        foreach (MangaInfo candidateManga in searchResult.Take(10))
        {
            chapters = await extension.GetChapters(candidateManga, ct);
            Assert.NotNull(chapters);
            if (chapters.Count > 0)
            {
                manga = candidateManga;
                break;
            }
        }
        Assert.NotNull(chapters);
        Assert.SkipWhen(chapters.Count == 0, $"{candidate.Name} exposes no chapters for any of its first results.");

        // GetChapterImages, not FetchChapterImages: the default interface method also runs the JPEG conversion the
        // download pipeline depends on, so this covers what DownloadChapterTask actually calls.
        List<ChapterImage>? images = await extension.GetChapterImages(chapters[^1], ct);
        Assert.NotNull(images);
        Assert.NotEmpty(images);
        Assert.All(images, image => Assert.True(image.image.Length > 0));
    }

    [Fact]
    public async Task RefreshIsIdempotent()
    {
        await SkipUnlessReachable();

        int first = await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(ct);
        int second = await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(ct);
        Assert.Equal(first, second);
        Assert.Distinct(DownloadExtensionsCollection.Extensions.Select(e => e.Identifier));
    }
}

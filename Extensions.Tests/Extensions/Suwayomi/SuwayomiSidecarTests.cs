using Common.Datatypes;
using Extensions.Data;
using Extensions.Extensions.Suwayomi;

namespace Extensions.Tests.Extensions.Suwayomi;

/// <summary>
/// Live tests against a running Suwayomi sidecar. They skip unless <c>ENABLE_SUWAYOMI</c> is set, because unlike the
/// other extension tests in this project the dependency is a container the developer has to bring up, not a public site.
/// </summary>
public sealed class SuwayomiSidecarTests : Common.Tests.TrangaTest
{
    private static void SkipIfDisabled()
    {
        Assert.SkipUnless(SuwayomiSource.IsAvailable,
            "Set ENABLE_SUWAYOMI=true and run the suwayomi container to exercise these tests.");
    }

    [Fact]
    public async Task SidecarIsReachable()
    {
        SkipIfDisabled();

        SuwayomiStatus status = await SuwayomiExtensionManager.GetStatusAsync(ct);
        Assert.True(status.Enabled);
        Assert.True(status.Reachable);
        Assert.False(string.IsNullOrEmpty(status.ServerVersion));
    }

    [Fact]
    public async Task ExtensionCatalogueIsPopulated()
    {
        SkipIfDisabled();

        // Requires the keiyoushi store to be configured, which the AppHost does via EXTENSION_STORES.
        SuwayomiExtensionInfo[]? extensions = await SuwayomiExtensionManager.GetExtensionsAsync(refresh: true, ct);
        Assert.NotNull(extensions);
        Assert.NotEmpty(extensions);
    }

    [Fact]
    public async Task InstalledSourcesBecomeDownloadExtensions()
    {
        SkipIfDisabled();

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
        SkipIfDisabled();

        await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(ct);

        // The built-in "Local source" has no homeUrl and serves files from the sidecar's own disk, so it cannot stand
        // in for a real remote source here.
        SuwayomiSourceInfo[] sources = await SuwayomiExtensionManager.GetSourcesAsync(ct) ?? [];
        SuwayomiSourceInfo? candidate = sources.FirstOrDefault(s => !string.IsNullOrEmpty(s.HomeUrl) && !s.IsNsfw);
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

        List<ChapterInfo>? chapters = await extension.GetChapters(manga, ct);
        Assert.NotNull(chapters);
        Assert.NotEmpty(chapters);

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
        SkipIfDisabled();

        int first = await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(ct);
        int second = await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(ct);
        Assert.Equal(first, second);
        Assert.Distinct(DownloadExtensionsCollection.Extensions.Select(e => e.Identifier));
    }
}

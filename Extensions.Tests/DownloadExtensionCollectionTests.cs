using Common.Datatypes;
using Common.Helpers;
using Extensions.Data;
using Extensions.Extensions;

namespace Extensions.Tests;

public sealed class DownloadExtensionCollectionTests : Common.Tests.TrangaTest
{
    [Fact]
    public void UniqueExtensionIds()
    {
        Assert.Distinct(DownloadExtensionsCollection.Extensions.Select(e => e.Identifier));
    }

    [Fact]
    public void BuiltInExtensionsAreAvailableWithoutRefresh()
    {
        Assert.Contains(DownloadExtensionsCollection.Extensions, e => e is MangaDex);
    }

    [Fact]
    public void GetExtensionReturnsNullForUnknownId()
    {
        Assert.Null(DownloadExtensionsCollection.GetExtension(Guid.NewGuid()));
    }

    [Fact]
    public async Task BuiltInProviderProvidesMangaDex()
    {
        IReadOnlyList<IDownloadExtension> extensions = await new BuiltInDownloadExtensionProvider().LoadAsync(ct);
        Assert.Contains(extensions, e => e is MangaDex);
    }

    [Fact]
    public async Task SearchSkipsExtensionsThatThrow()
    {
        FakeExtension working = new(Guid.NewGuid(), [new MangaInfo(Guid.Empty, "Working", "https://example.com/1", "1", new TrangaImage())]);
        FakeExtension throwing = new(Guid.NewGuid(), throws: true);

        List<MangaInfo> result = DownloadExtensionsCollection.Search(new SearchQuery { Title = "x" }, [throwing, working], ct);

        MangaInfo manga = Assert.Single(result);
        Assert.Equal("Working", manga.Title);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RefreshAddsExtensionsFromRegisteredProviders()
    {
        Guid id = Guid.NewGuid();
        DownloadExtensionsCollection.RegisterProvider(new FakeProvider(new FakeExtension(id)));
        // A provider that throws must not hide the extensions of the other providers.
        DownloadExtensionsCollection.RegisterProvider(new FakeProvider(throws: true));

        await DownloadExtensionsCollection.RefreshAsync(ct);

        Assert.NotNull(DownloadExtensionsCollection.GetExtension(id));
        Assert.Contains(DownloadExtensionsCollection.Extensions, e => e is MangaDex);
        Assert.Distinct(DownloadExtensionsCollection.Extensions.Select(e => e.Identifier));
    }

    [Fact]
    public async Task SearchDefaultsToDownloadLanguageWhenQueryHasNoLanguage()
    {
        // Individual extensions should never see a null Language - Search fills it in from
        // Settings.DownloadLanguage before dispatching, so every extension defaults consistently.
        SearchQuery searchQuery = new()
        {
            Title = "Sousou no Frieren"
        };
        List<MangaInfo> result = DownloadExtensionsCollection.Search(searchQuery, [new MangaDex()], ct);
        MangaInfo? manga = result.FirstOrDefault(r => r.Url == "https://mangadex.org/title/b0b721ff-c388-4486-aa0f-c2b0bb321512");
        Assert.NotNull(manga);
        Assert.Equal("Sousou no Frieren", manga.Title);
        await Task.CompletedTask;
    }

    private sealed class FakeProvider(IDownloadExtension? extension = null, bool throws = false) : IDownloadExtensionProvider
    {
        public string Name => "Fake";

        public Task<IReadOnlyList<IDownloadExtension>> LoadAsync(CancellationToken ct) =>
            throws
                ? throw new InvalidOperationException("provider failed")
                : Task.FromResult<IReadOnlyList<IDownloadExtension>>(extension is null ? [] : [extension]);
    }

    private sealed class FakeExtension(Guid identifier, List<MangaInfo>? results = null, bool throws = false) : IDownloadExtension
    {
        public Guid Identifier { get; init; } = identifier;
        public string Name { get; init; } = "Fake";
        public string BaseUrl { get; init; } = "https://example.com";
        public string IconUrl { get; init; } = "https://example.com/icon.png";
        public Language[] SupportedLanguages { get; init; } = ["en"!];

        public Task<List<MangaInfo>?> SearchDownload(SearchQuery query, CancellationToken ct) =>
            throws ? throw new InvalidOperationException("extension failed") : Task.FromResult<List<MangaInfo>?>(results);

        public string? ParseIdentifierFromUrl(string url) => null;

        public Task<List<ChapterInfo>?> GetChapters(MangaInfo mangaInfo, CancellationToken ct) =>
            Task.FromResult<List<ChapterInfo>?>(null);

        public Task<List<ChapterImage>?> FetchChapterImages(ChapterInfo chapterInfo, CancellationToken ct) =>
            Task.FromResult<List<ChapterImage>?>(null);
    }
}

using Common.Datatypes;
using Common.Helpers;
using Extensions;
using Extensions.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Services.Manga.Tests.Helpers;

/// <summary>
/// A download extension without a website, registered with the <see cref="DownloadExtensionsCollection"/> for the
/// endpoints that look an extension up by its id. Every URL below <see cref="SeriesUrlPrefix"/> is a series (the
/// identifier is the URL).
/// </summary>
internal sealed class FakeDownloadExtension : IDownloadExtension
{
    public const string SeriesUrlPrefix = "https://fake.test/series/";

    public Guid Identifier { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "Fake";
    public string BaseUrl { get; init; } = "https://fake.test";
    public string IconUrl { get; init; } = "https://fake.test/icon.png";
    public Language[] SupportedLanguages { get; init; } = ["en"!];

    /// <summary>What <see cref="GetChapters"/> returns. null means the series cannot be read.</summary>
    public List<ChapterInfo>? Chapters { get; init; } = [];

    /// <summary>Whether the extension has a cover for a series. If not, <see cref="GetCover"/> returns null.</summary>
    public bool HasCover { get; init; } = true;

    /// <summary>Whether <see cref="GetCover"/> fails.</summary>
    public bool CoverThrows { get; init; }

    /// <summary>How often <see cref="GetCover"/> was called.</summary>
    public int CoverRequests { get; private set; }

    public Task<List<MangaInfo>?> SearchDownload(SearchQuery query, CancellationToken ct) => Task.FromResult<List<MangaInfo>?>([]);

    public string? ParseIdentifierFromUrl(string url) => url.StartsWith(SeriesUrlPrefix, StringComparison.Ordinal) ? url : null;

    public Task<List<ChapterInfo>?> GetChapters(MangaInfo mangaInfo, CancellationToken ct) => Task.FromResult(Chapters);

    public Task<TrangaImage?> GetCover(MangaInfo mangaInfo, CancellationToken ct)
    {
        CoverRequests++;
        if (CoverThrows)
            throw new InvalidOperationException("cover failed");
        return Task.FromResult(HasCover ? Jpeg() : null);
    }

    public Task<List<ChapterImage>?> FetchChapterImages(ChapterInfo chapterInfo, CancellationToken ct) =>
        Task.FromResult<List<ChapterImage>?>(null);

    /// <summary>A valid, tiny JPEG as the extension's download of a cover.</summary>
    public static TrangaImage Jpeg()
    {
        using Image<Rgb24> image = new(4, 4);
        TrangaImage result = new();
        image.SaveAsJpeg(result);
        result.Position = 0;
        return result;
    }

    /// <summary>
    /// Makes <paramref name="extension"/> available to <see cref="DownloadExtensionsCollection.GetExtension"/>. Safe to
    /// call from tests that run in parallel: the collection is process-wide and a refresh replaces it as a whole, so
    /// every extension registered so far is part of every refresh.
    /// </summary>
    public static async Task Register(FakeDownloadExtension extension, CancellationToken ct)
    {
        lock (Provider.Extensions)
        {
            if (Provider.Extensions.Count == 0)
                DownloadExtensionsCollection.RegisterProvider(Provider);
            Provider.Extensions.Add(extension);
        }

        // Refreshes one after the other, so that the last one has seen everything registered before it.
        await Provider.RefreshLock.WaitAsync(ct);
        try
        {
            await DownloadExtensionsCollection.RefreshAsync(ct);
        }
        finally
        {
            Provider.RefreshLock.Release();
        }
    }

    private static readonly FakeProvider Provider = new();

    private sealed class FakeProvider : IDownloadExtensionProvider
    {
        public List<IDownloadExtension> Extensions { get; } = [];
        public SemaphoreSlim RefreshLock { get; } = new(1, 1);

        public string Name => "Fake";

        public Task<IReadOnlyList<IDownloadExtension>> LoadAsync(CancellationToken ct)
        {
            lock (Extensions)
                return Task.FromResult<IReadOnlyList<IDownloadExtension>>([.. Extensions]);
        }
    }
}

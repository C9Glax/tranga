using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Common.Datatypes;
using Common.Helpers;
using Common.Settings;
using Extensions.Data;

namespace Extensions.Extensions.Suwayomi;

/// <summary>
/// A single source exposed by an extension installed on the Suwayomi sidecar — one instance per source, created at
/// runtime by <see cref="DiscoverAsync"/> rather than compiled into <see cref="DownloadExtensionsCollection"/>.
/// <para>
/// This is how Tranga reaches the ~2000 sources of the
/// <see href="https://github.com/keiyoushi/extensions">keiyoushi</see> repository: those extensions are Android APKs
/// built against Tachiyomi's <c>HttpSource</c> API and cannot be loaded from .NET, so Suwayomi runs them on the JVM and
/// Tranga drives it over GraphQL.
/// </para>
/// </summary>
public sealed class SuwayomiSource : IDownloadExtension
{
    /// <inheritdoc />
    public Guid Identifier { get; init; }

    /// <inheritdoc />
    public string Name { get; init; }

    /// <inheritdoc />
    public string BaseUrl { get; init; }

    /// <inheritdoc />
    public string IconUrl { get; init; }

    /// <inheritdoc />
    public Language[] SupportedLanguages { get; init; }

    /// <summary>The Tachiyomi source id, as a string because it does not fit a signed 32-bit integer.</summary>
    private readonly string _sourceId;

    private readonly SuwayomiContentWarning _contentWarning;

    /// <summary>
    /// Genre tags that mark an individual entry as adult. Tachiyomi's manga model carries no content rating of its own —
    /// the classification exists only per source — so for a <see cref="SuwayomiContentWarning.Mixed"/> source the tags
    /// are the only per-entry signal available. Deliberately broad: while NSFW is disallowed, showing adult content is
    /// the worse failure than hiding a borderline entry.
    /// </summary>
    private static readonly HashSet<string> AdultGenres = new(StringComparer.OrdinalIgnoreCase)
    {
        "hentai", "adult", "smut", "erotica", "pornographic", "porn", "nsfw", "18+", "r18", "r-18", "mature"
    };

    /// <summary>
    /// Namespace for the version-5 UUIDs identifying Suwayomi-backed extensions. It must never change: the derived
    /// <see cref="Identifier"/>s are persisted against download links, so a new namespace would orphan every existing row.
    /// </summary>
    private static readonly Guid SuwayomiNamespace = Guid.Parse("6d2c1f6a-4a6b-4f2f-9d3b-5a1c7e0b8f41");

    /// <summary>Separates the manga url from the chapter url inside <see cref="ChapterInfo.Identifier"/>; a control character that cannot occur in a url.</summary>
    private const char IdentifierSeparator = '\u001F';

    /// <param name="sourceId">The Tachiyomi source id reported by Suwayomi.</param>
    /// <param name="name">Display name of the source, normally including its language.</param>
    /// <param name="homeUrl">The source's own website, used to parse manga identifiers out of pasted urls. May be empty.</param>
    /// <param name="iconUrl">Gateway-relative url of the source's icon.</param>
    /// <param name="lang">Tachiyomi language code, e.g. <c>en</c>, <c>pt-BR</c>, <c>all</c>.</param>
    /// <param name="contentWarning">How the extension store classifies the source's content.</param>
    public SuwayomiSource(string sourceId, string name, string homeUrl, string iconUrl, string lang, SuwayomiContentWarning contentWarning)
    {
        _sourceId = sourceId;
        _contentWarning = contentWarning;
        Identifier = IdentifierFor(sourceId);
        Name = name;
        BaseUrl = homeUrl;
        IconUrl = iconUrl;
        SupportedLanguages = ParseLanguages(lang);
    }

    /// <summary>
    /// Derives this extension's stable <see cref="IExtension.Identifier"/> from a Tachiyomi source id, as a version-5
    /// UUID. Deterministic on purpose: the identifier is written to download-link rows, so it has to survive restarts,
    /// re-installs of the extension and a rebuilt sidecar.
    /// </summary>
    public static Guid IdentifierFor(string sourceId)
    {
        byte[] namespaceBytes = SuwayomiNamespace.ToByteArray(bigEndian: true);
        byte[] nameBytes = Encoding.UTF8.GetBytes(sourceId);
        byte[] hash = SHA1.HashData([.. namespaceBytes, .. nameBytes]);

        Span<byte> guidBytes = hash.AsSpan(0, 16);
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50); // version 5
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80); // RFC 4122 variant
        return new Guid(guidBytes, bigEndian: true);
    }

    /// <summary>
    /// Returns one extension per source currently installed on the sidecar, or an empty array when the sidecar is
    /// unreachable.
    /// </summary>
    public static async Task<IDownloadExtension[]> DiscoverAsync(CancellationToken ct)
    {
        if (await SuwayomiClient.GetSourcesAsync(ct) is not { } sources)
            return [];

        // NSFW sources stay registered even when NSFW is disallowed, so download links pointing at them keep resolving;
        // it is searching that is suppressed (see SearchDownload).
        return sources
            .Select(source => (IDownloadExtension)new SuwayomiSource(
                source.Id,
                string.IsNullOrWhiteSpace(source.DisplayName) ? source.Name : source.DisplayName,
                source.HomeUrl ?? string.Empty,
                SuwayomiClient.ToIconUrl(source.IconUrl),
                source.Lang,
                ParseContentWarning(source.ContentWarning)))
            .ToArray();
    }

    /// <summary>
    /// Maps Suwayomi's <c>contentWarning</c> enum name onto <see cref="SuwayomiContentWarning"/>.
    /// Anything unrecognised is treated as <see cref="SuwayomiContentWarning.Mixed"/>: the source stays usable, but its
    /// entries are still tag-filtered while NSFW is disallowed.
    /// </summary>
    public static SuwayomiContentWarning ParseContentWarning(string? contentWarning) =>
        Enum.TryParse(contentWarning, ignoreCase: true, out SuwayomiContentWarning parsed)
            ? parsed
            : SuwayomiContentWarning.Mixed;

    /// <summary>
    /// Whether an individual entry counts as adult content: everything from an
    /// <see cref="SuwayomiContentWarning.Nsfw"/> source does, and elsewhere it is decided by the entry's genre tags.
    /// </summary>
    public static bool IsAdultContent(SuwayomiContentWarning contentWarning, IEnumerable<string>? genres)
    {
        if (contentWarning is SuwayomiContentWarning.Nsfw)
            return true;
        return genres?.Any(genre => AdultGenres.Contains(genre.Trim())) is true;
    }

    #region Search

    /// <inheritdoc />
    public async Task<List<MangaInfo>?> SearchDownload(SearchQuery query, CancellationToken ct)
    {
        // Only dedicated adult sources are locked out entirely; a mixed source stays searchable and has its individual
        // results filtered below.
        if (_contentWarning is SuwayomiContentWarning.Nsfw && !Settings.AllowNSFW)
            return [];

        if (await SuwayomiClient.SearchSourceAsync(_sourceId, query.Title ?? string.Empty, 1, ct) is not { } mangas)
            return null;

        // Filtered before mapping, because mapping downloads a cover per entry.
        IEnumerable<SuwayomiMangaDto> allowed = Settings.AllowNSFW
            ? mangas
            : mangas.Where(manga => !IsAdultContent(_contentWarning, manga.Genre));

        List<Task<MangaInfo?>> tasks = allowed.Select(manga => ToMangaInfo(manga, ct)).ToList();
        await Task.WhenAll(tasks);

        return tasks
            .Where(t => t is { IsCompletedSuccessfully: true, Result: not null })
            .Select(t => t.Result!)
            .ToList();
    }

    private async Task<MangaInfo?> ToMangaInfo(SuwayomiMangaDto manga, CancellationToken ct)
    {
        // A manga without a usable cover is dropped rather than surfaced blank, matching MangaDex's behaviour.
        if (await SuwayomiClient.GetImageAsync(manga.ThumbnailUrl, ct) is not { } cover)
            return null;

        return new MangaInfo(
            Identifier,
            manga.Title,
            manga.RealUrl ?? $"{BaseUrl}{manga.Url}",
            manga.Url,
            cover,
            manga.Description,
            IsAdultContent(_contentWarning, manga.Genre));
    }

    #endregion

    #region Identifier

    /// <inheritdoc />
    public string? ParseIdentifierFromUrl(string url)
    {
        if (string.IsNullOrEmpty(BaseUrl))
            return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return null;
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out Uri? baseUri))
            return null;
        if (!uri.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase))
            return null;

        // Suwayomi identifies a manga by the source-relative path, which is exactly what the source's own urls carry.
        string identifier = uri.PathAndQuery;
        return identifier is "" or "/" ? null : identifier;
    }

    #endregion

    #region Chapters

    /// <inheritdoc />
    public async Task<List<ChapterInfo>?> GetChapters(MangaInfo mangaInfo, CancellationToken ct)
    {
        string mangaUrl = mangaInfo.Identifier;
        if (await SuwayomiClient.ResolveMangaAsync(_sourceId, mangaUrl, ct) is not { } manga)
            return null;
        if (await SuwayomiClient.FetchChaptersAsync(manga.Id, ct) is not { } chapters)
            return null;

        return chapters
            .Select(chapter => new ChapterInfo(
                Identifier,
                FormatChapterNumber(chapter),
                $"{BaseUrl}{chapter.Url}",
                // The manga url is carried alongside the chapter url so a chapter can still be resolved when the
                // sidecar has forgotten it (its ids are row ids, and only the urls are stable).
                $"{mangaUrl}{IdentifierSeparator}{chapter.Url}",
                Title: string.IsNullOrWhiteSpace(chapter.Name) ? null : chapter.Name))
            .ToList();
    }

    private static string FormatChapterNumber(SuwayomiChapterDto chapter) =>
        chapter.ChapterNumber >= 0
            ? chapter.ChapterNumber.ToString("0.####", CultureInfo.InvariantCulture)
            // Sources that do not expose a chapter number report -1; fall back to the source's own ordering.
            : (chapter.SourceOrder + 1).ToString(CultureInfo.InvariantCulture);

    #endregion

    #region Images

    /// <inheritdoc />
    public async Task<List<ChapterImage>?> FetchChapterImages(ChapterInfo chapterInfo, CancellationToken ct)
    {
        string[] parts = chapterInfo.Identifier.Split(IdentifierSeparator);
        if (parts.Length != 2)
            return null;
        string mangaUrl = parts[0];
        string chapterUrl = parts[1];

        if (await SuwayomiClient.ResolveMangaAsync(_sourceId, mangaUrl, ct) is not { } manga)
            return null;

        SuwayomiChapterDto? chapter = await SuwayomiClient.ResolveChapterAsync(manga.Id, chapterUrl, ct);
        if (chapter is null)
        {
            // Chapter rows only exist once a chapter list has been fetched at least once; repopulate and retry.
            if (await SuwayomiClient.FetchChaptersAsync(manga.Id, ct) is null)
                return null;
            chapter = await SuwayomiClient.ResolveChapterAsync(manga.Id, chapterUrl, ct);
        }
        if (chapter is null)
            return null;

        if (await SuwayomiClient.GetChapterPagesAsync(chapter.Id, ct) is not { Length: > 0 } pages)
            return null;

        List<Task<TrangaImage?>> tasks = pages.Select(page => SuwayomiClient.GetImageAsync(page, ct)).ToList();
        await Task.WhenAll(tasks);

        if (tasks.Any(t => t is not { IsCompletedSuccessfully: true, Result: not null }))
            return null;

        return tasks
            .Select((t, index) => new ChapterImage(Identifier, chapterInfo.Identifier, index, t.Result!))
            .ToList();
    }

    #endregion

    #region Utilities

    private static Language[] ParseLanguages(string lang)
    {
        // Tachiyomi uses pseudo-codes ("all", "other", "localsourcelang") for multi-language and unclassified sources.
        // ICU happily manufactures a CultureInfo for names like "all", so the culture has to be checked against the
        // predefined set rather than relying on the constructor to throw.
        if (string.IsNullOrWhiteSpace(lang))
            return [];
        try
        {
            CultureInfo.GetCultureInfo(lang, predefinedOnly: true);
            return [new Language(lang)];
        }
        catch (CultureNotFoundException)
        {
            return [];
        }
    }

    #endregion
}

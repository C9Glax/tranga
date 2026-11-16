using System.Globalization;
using System.Threading.RateLimiting;
using Common.Datatypes;
using Common.Helpers;
using Common.Settings;
using Extensions.Data;
using Extensions.Extensions.MangaPlusApi;
using Extensions.Helpers;
using Google.Protobuf;

namespace Extensions.Extensions;

/// <summary>
/// Download extension for <c>mangaplus.shueisha.co.jp</c> (Shueisha's official, free-to-read MANGA Plus).
/// <para>
/// A port of the keiyoushi Tachiyomi extension <c>src/all/mangaplus</c>. The site is a SPA on top of a protobuf API
/// (<c>jumpg-webapi.tokyo-cdn.com</c>); see <c>MangaPlusApi/mangaplus.proto</c> for the schema. Two quirks matter:
/// <list type="bullet">
/// <item>every API request carries a random <c>SESSION-TOKEN</c> header,</item>
/// <item>page images are served XOR-encrypted with a per-page hex key, and are only served with the chapter's
/// <c>Plus-Vw-Token</c> header.</item>
/// </list>
/// MANGA Plus publishes one catalogue per language; the language used is <see cref="Settings.DownloadLanguage"/>
/// (or the query's language when searching).
/// </para>
/// </summary>
public sealed class MangaPlus : IDownloadExtension
{
    public Guid Identifier { get; init; } = Guid.Parse("73f2aa03-cffd-4021-82ef-eadf2e3b4e0f");

    public string Name { get; init; } = "MANGA Plus";

    public Language[] SupportedLanguages { get; init; } =
        ["en"!, "es"!, "fr"!, "id"!, "pt-BR"!, "ru"!, "th"!, "de"!, "vi"!];

    public string BaseUrl { get; init; } = "https://mangaplus.shueisha.co.jp";

    public string IconUrl { get; init; } = "https://mangaplus.shueisha.co.jp/favicon.ico";

    private const string ApiUrl = "https://jumpg-webapi.tokyo-cdn.com/api";
    private const int MaxSearchResults = 10;

    // Identifies this process to the API; the real web client generates one per page load.
    private static readonly string SessionToken = Guid.NewGuid().ToString();

    // The keiyoushi extension limits API calls to 1/s; the image CDN is not throttled that hard.
    private static readonly RequestClient ApiClient = new(new SlidingWindowRateLimiter(
        new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1,
            PermitLimit = 1,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));

    private static readonly RequestClient ImageClient = new(new SlidingWindowRateLimiter(
        new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1,
            PermitLimit = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));

    #region Languages

    private static readonly Dictionary<string, MangaPlusLanguage> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new("eng", 0),
        ["es"] = new("esp", 1),
        ["fr"] = new("fra", 2),
        ["id"] = new("ind", 3),
        ["pt-BR"] = new("ptb", 4),
        ["pt"] = new("ptb", 4), // MANGA Plus only publishes Brazilian Portuguese
        ["ru"] = new("rus", 5),
        ["th"] = new("tha", 6),
        ["de"] = new("deu", 7),
        ["vi"] = new("vie", 9)
    };

    /// <summary>The API's name for a language (used in query strings) and the code it tags titles with.</summary>
    internal readonly record struct MangaPlusLanguage(string Name, int Code);

    internal static bool TryGetLanguage(Language? language, out MangaPlusLanguage result)
    {
        result = default;
        if (language is null)
            return false;
        return Languages.TryGetValue(language.Name, out result) ||
               Languages.TryGetValue(language.TwoLetterISOLanguageName, out result);
    }

    #endregion

    #region Search

    public async Task<List<MangaInfo>?> SearchDownload(SearchQuery query, CancellationToken ct)
    {
        if (!TryGetLanguage(query.Language ?? Settings.DownloadLanguage, out MangaPlusLanguage language))
            return [];
        if (string.IsNullOrWhiteSpace(query.Title))
            return [];

        // The site has no search endpoint: the web client downloads the whole catalogue and filters locally.
        if (await GetApi("title_list/allV2", ct) is not { Success.AllTitlesView: { } allTitles })
            return null;

        string term = query.Title.Trim();
        List<Title> matches = allTitles.AllTitlesGroup
            .SelectMany(group => group.Titles)
            .Where(title => title.TitleId != 0 && title.Language == language.Code)
            .DistinctBy(title => title.TitleId)
            .Where(title => title.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            title.Author.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(title => title.Name.StartsWith(term, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(title => title.Name, StringComparer.OrdinalIgnoreCase)
            .Take(MaxSearchResults)
            .ToList();

        MangaInfo?[] results = await Task.WhenAll(matches.Select(title => ParseMangaInfo(title, ct)));
        return results.OfType<MangaInfo>().ToList();
    }

    private async Task<MangaInfo?> ParseMangaInfo(Title title, CancellationToken ct)
    {
        if (await DownloadImage(ImageClient, title.PortraitImageUrl, null, null, ct) is not { } cover)
            return null;
        // MANGA Plus is an all-ages publisher, so nothing on it is flagged NSFW.
        return new MangaInfo(Identifier, title.Name, $"{BaseUrl}/titles/{title.TitleId}", title.TitleId.ToString(), cover,
            Description: null, NSFW: false);
    }

    #endregion

    #region Identifier

    public string? ParseIdentifierFromUrl(string url)
    {
        // https://mangaplus.shueisha.co.jp/titles/100020
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Segments.Length < 3)
            return null;
        if (!uri.Host.Equals("mangaplus.shueisha.co.jp", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.Equals("www.mangaplus.shueisha.co.jp", StringComparison.OrdinalIgnoreCase))
            return null;
        if (!uri.Segments[1].Equals("titles/", StringComparison.OrdinalIgnoreCase))
            return null;
        string identifier = uri.Segments[2].Trim('/');
        return int.TryParse(identifier, NumberStyles.None, CultureInfo.InvariantCulture, out _) ? identifier : null;
    }

    #endregion

    #region Chapters

    public async Task<List<ChapterInfo>?> GetChapters(MangaInfo mangaInfo, CancellationToken ct)
    {
        MangaPlusLanguage language = CurrentLanguage();
        if (await GetApi($"title_detailV3?title_id={mangaInfo.Identifier}&clang={language.Name}", ct) is not
            { Success.TitleDetailView: { } detail })
            return null;

        // The title exists, but in another language's catalogue.
        if (detail.Title.Language != language.Code)
            return [];

        return ParseChapters(detail);
    }

    internal List<ChapterInfo> ParseChapters(TitleDetailView detail) => detail.ChapterListGroup
        .SelectMany(group => group.FirstChapterList.Concat(group.LastChapterList))
        .Where(chapter => chapter.HasSubTitle) // no subtitle == expired, can't be read anymore
        .DistinctBy(chapter => chapter.ChapterId)
        .OrderBy(chapter => chapter.StartTimeStamp)
        .Select(chapter => (chapter, number: ParseChapterNumber(chapter.Name)))
        .Where(entry => entry.number is not null) // extras and one-shots have no number
        .Select(entry => new ChapterInfo(Identifier, entry.number!, $"{BaseUrl}/viewer/{entry.chapter.ChapterId}",
            entry.chapter.ChapterId.ToString(), Volume: null, Title: entry.chapter.SubTitle,
            ReleaseDate: DateTimeOffset.FromUnixTimeSeconds(entry.chapter.StartTimeStamp)))
        .ToList();

    /// <summary>"#012" -> "12", "#12.5" -> "12.5", "ex" -> null</summary>
    internal static string? ParseChapterNumber(string name)
    {
        string number = name[(name.IndexOf('#') + 1)..];
        return decimal.TryParse(number, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed) && parsed >= 0
            ? parsed.ToString(CultureInfo.InvariantCulture)
            : null;
    }

    #endregion

    #region Images

    public async Task<List<ChapterImage>?> FetchChapterImages(ChapterInfo chapterInfo, CancellationToken ct)
    {
        MangaPlusLanguage language = CurrentLanguage();
        // split=no keeps double-page spreads in one piece, which is what you want in an archive (readers can split).
        if (await GetApi(
                $"manga_viewer_v3?chapter_id={chapterInfo.Identifier}&split=no&img_quality=super_high&clang={language.Name}",
                ct) is not { Success.MangaViewer: { } viewer })
            return null;

        List<MangaPage> pages = viewer.Pages
            .Where(page => page.MangaPage is not null)
            .Select(page => page.MangaPage)
            .ToList();
        if (pages.Count == 0)
            return null;

        TrangaImage?[] downloaded = await Task.WhenAll(pages.Select(page =>
            DownloadImage(ImageClient, page.ImageUrl, viewer.ViewToken, page.EncryptionKey, ct)));

        // A chapter with a missing page is worse than no chapter, so any failed page fails the whole chapter.
        if (downloaded.Any(image => image is null))
            return null;

        return downloaded
            .Select((image, index) => new ChapterImage(Identifier, chapterInfo.Identifier, index, image!))
            .ToList();
    }

    /// <summary>
    /// Downloads one image, retrying with a short backoff, and undoes the XOR encryption when a key is given.
    /// Returns null when all attempts fail.
    /// </summary>
    private static Task<TrangaImage?> DownloadImage(RequestClient client, string url, string? viewToken,
        string? encryptionKey, CancellationToken ct) =>
        ImageDownloader.DownloadAsync(client, () =>
        {
            HttpRequestMessage request = new(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(viewToken))
                request.Headers.Add("Plus-Vw-Token", viewToken);
            return request;
        }, string.IsNullOrEmpty(encryptionKey) ? null : data => Xor(data, Convert.FromHexString(encryptionKey)), ct);

    /// <summary>XORs <paramref name="data"/> in place with the repeating <paramref name="key"/>. Applying it twice restores the input.</summary>
    internal static void Xor(Span<byte> data, ReadOnlySpan<byte> key)
    {
        if (key.IsEmpty)
            return;
        for (int i = 0; i < data.Length; i++)
            data[i] ^= key[i % key.Length];
    }

    #endregion

    #region Api

    private static MangaPlusLanguage CurrentLanguage() =>
        TryGetLanguage(Settings.DownloadLanguage, out MangaPlusLanguage language) ? language : Languages["en"];

    /// <returns>The parsed response when it carries a success payload, otherwise null.</returns>
    private static async Task<MangaPlusResponse?> GetApi(string pathAndQuery, CancellationToken ct)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"{ApiUrl}/{pathAndQuery}");
            request.Headers.Add("SESSION-TOKEN", SessionToken);
            using HttpResponseMessage response = await ApiClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return null;
            MangaPlusResponse parsed = MangaPlusResponse.Parser.ParseFrom(await response.Content.ReadAsByteArrayAsync(ct));
            return parsed.Success is null ? null : parsed;
        }
        catch (Exception e) when (!ct.IsCancellationRequested && e is HttpRequestException or InvalidProtocolBufferException)
        {
            return null;
        }
    }

    #endregion
}

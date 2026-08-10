using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Common.Datatypes;
using Common.Helpers;
using Common.Settings;
using Extensions.Data;

namespace Extensions.Extensions;

/// <summary>
/// Ported from https://github.com/keiyoushi/extensions-source/tree/main/src/all/mangaplus
/// The upstream site serves a binary protobuf API rather than HTML/JSON, so this extension
/// includes a small hand-rolled protobuf wire-format reader scoped to the handful of message
/// types actually needed (see the Protobuf region) instead of taking on a full protobuf library.
/// </summary>
public sealed class MangaPlus : IDownloadExtension, IMetadataExtension
{
    public Guid Identifier { get; init; } = Guid.Parse("0bc30bc2-dbcf-47ce-a890-c8428a7e031b");

    public string Name { get; init; } = "MangaPlus";

    public Language[] SupportedLanguages { get; init; } = ["en"!, "es"!, "fr"!, "id"!, "pt-BR"!, "ru"!, "th"!, "de"!, "vi"!];

    public string BaseUrl { get; init; } = "https://mangaplus.shueisha.co.jp";

    private const string ApiUrl = "https://jumpg-webapi.tokyo-cdn.com/api";

    private static readonly RequestClient RequestClient = new(new SlidingWindowRateLimiter(
        new SlidingWindowRateLimiterOptions()
        {
            AutoReplenishment = true,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1,
            PermitLimit = 1,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));

    // Mirrors the upstream source's per-instance UUID, sent as SESSION-TOKEN on every API request.
    private static readonly string SessionToken = Guid.NewGuid().ToString();

    private static readonly Dictionary<string, int> LanguageCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = 0,
        ["es"] = 1,
        ["fr"] = 2,
        ["id"] = 3,
        ["pt-BR"] = 4,
        ["ru"] = 5,
        ["th"] = 6,
        ["de"] = 7,
        ["vi"] = 9
    };

    private static int LanguageCode(string? iso)
    {
        string code = iso ?? Settings.DownloadLanguage.Name;
        if (LanguageCodes.TryGetValue(code, out int exact))
            return exact;
        try
        {
            string twoLetter = new CultureInfo(code).TwoLetterISOLanguageName;
            if (LanguageCodes.TryGetValue(twoLetter, out int byIso))
                return byIso;
        }
        catch (CultureNotFoundException)
        {
            // fall through to default
        }
        return 0;
    }

    #region Search

    private async Task<List<Title>?> GetTitles(SearchQuery query, CancellationToken ct)
    {
        int languageCode = LanguageCode(query.Language);
        if (await GetSuccess(new Uri($"{ApiUrl}/title_list/allV2"), ct) is not { AllTitlesView: { } view })
            return null;

        List<Title> titles = view.Groups
            .SelectMany(g => g.Titles)
            .Where(t => t.TitleId != 0 && t.Language == languageCode)
            .DistinctBy(t => t.TitleId)
            .ToList();

        // The upstream API has no server-side search - it filters the full catalog client-side by
        // matching a single search term against the title name or author, same as here.
        string? text = query.Title ?? query.Author;
        if (!string.IsNullOrEmpty(text))
            titles = titles
                .Where(t => t.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                            (t.Author?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

        return titles;
    }

    public async Task<List<MangaInfo>?> SearchDownload(SearchQuery query, CancellationToken ct)
    {
        if (await GetTitles(query, ct) is not { } titles)
            return null;

        List<Task<MangaInfo?>> tasks = titles.Select(t => ParseMangaInfo(t, ct)).ToList();
        await Task.WhenAll(tasks);

        return tasks
            .Where(t => t is { IsCompletedSuccessfully: true, Result: not null })
            .Select(t => t.Result!)
            .ToList();
    }

    private async Task<MangaInfo?> ParseMangaInfo(Title title, CancellationToken ct)
    {
        if (await FetchImage(title.PortraitImageUrl, ct) is not { } cover)
            return null;
        string url = $"{BaseUrl}/titles/{title.TitleId}";
        return new MangaInfo(this.Identifier, title.Name, url, title.TitleId.ToString(), cover);
    }

    public async Task<List<SearchResult>?> SearchMetadata(SearchQuery searchQuery, CancellationToken ct)
    {
        if (await GetTitles(searchQuery, ct) is not { } titles)
            return null;

        List<Task<SearchResult?>> tasks = titles.Select(t => ParseSearchResult(t, ct)).ToList();
        await Task.WhenAll(tasks);

        return tasks
            .Where(t => t is { IsCompletedSuccessfully: true, Result: not null })
            .Select(t => t.Result!)
            .ToList();
    }

    private async Task<SearchResult?> ParseSearchResult(Title title, CancellationToken ct)
    {
        if (await FetchImage(title.PortraitImageUrl, ct) is not { } cover)
            return null;
        string url = $"{BaseUrl}/titles/{title.TitleId}";
        return new SearchResult()
        {
            MetadataExtensionIdentifier = this.Identifier,
            Identifier = title.TitleId.ToString(),
            Series = title.Name,
            Cover = cover,
            Url = url,
            Authors = title.Author is { } author ? [author] : null
        };
    }

    #endregion

    #region Identifier

    public string? ParseIdentifierFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Segments.Length < 3)
            return null;
        if (!uri.Segments[1].Equals("titles/", StringComparison.OrdinalIgnoreCase))
            return null;
        string identifier = uri.Segments[2].Trim('/');
        return int.TryParse(identifier, out _) ? identifier : null;
    }

    #endregion

    #region Chapters

    public async Task<List<ChapterInfo>?> GetChapters(MangaInfo mangaInfo, CancellationToken ct)
    {
        if (!int.TryParse(mangaInfo.Identifier, out int titleId))
            return null;

        // clang only localizes secondary text (e.g. viewing-period descriptions); the titleId itself
        // already pins the series to one specific language edition, so a fixed value here is safe.
        Uri url = new($"{ApiUrl}/title_detailV3?title_id={titleId}&clang=eng");
        if (await GetSuccess(url, ct) is not { TitleDetailView: { } detail })
            return null;

        List<ChapterInfo> result = [];
        foreach (ChapterDto chapter in detail.Chapters.Where(c => !c.IsExpired))
        {
            int hashIndex = chapter.Name.IndexOf('#');
            string number = hashIndex >= 0 ? chapter.Name[(hashIndex + 1)..] : chapter.Name;
            string chapterUrl = $"{BaseUrl}/viewer/{chapter.ChapterId}";
            result.Add(new ChapterInfo(this.Identifier, number, chapterUrl, chapter.ChapterId.ToString(), Title: chapter.SubTitle));
        }

        return result;
    }

    #endregion

    #region Images

    public async Task<List<ChapterImage>?> FetchChapterImages(ChapterInfo chapterInfo, CancellationToken ct)
    {
        Uri url = new($"{ApiUrl}/manga_viewer_v3?chapter_id={chapterInfo.Identifier}&split=no&img_quality=super_high&clang=eng");
        if (await GetSuccess(url, ct) is not { MangaViewer: { } viewer })
            return null;

        List<MangaPageDto> pages = viewer.Pages
            .Select(p => p.MangaPage)
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();
        if (pages.Count == 0)
            return null;

        List<Task<TrangaImage?>> tasks = pages
            .Select(p => FetchImage(p.ImageUrl, p.EncryptionKey, viewer.ViewToken, ct))
            .ToList();
        await Task.WhenAll(tasks);

        if (tasks.Any(t => t is not { IsCompletedSuccessfully: true, Result: not null }))
            return null;

        return tasks
            .Select((t, index) => new ChapterImage(this.Identifier, chapterInfo.Identifier, index, t.Result!))
            .ToList();
    }

    #endregion

    #region Utilities

    private async Task<SuccessResultDto?> GetSuccess(Uri url, CancellationToken ct)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Add("SESSION-TOKEN", SessionToken);
        HttpResponseMessage response = await RequestClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        byte[] data = await response.Content.ReadAsByteArrayAsync(ct);
        return ParseMangaPlusResponse(data);
    }

    private Task<TrangaImage?> FetchImage(string url, CancellationToken ct) => FetchImage(url, null, null, ct);

    private async Task<TrangaImage?> FetchImage(string url, string? encryptionKey, string? viewToken, CancellationToken ct)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        if (viewToken is not null)
            request.Headers.Add("Plus-Vw-Token", viewToken);
        HttpResponseMessage response = await RequestClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        byte[] data = await response.Content.ReadAsByteArrayAsync(ct);
        if (encryptionKey is not null)
            data = Decrypt(data, encryptionKey);

        TrangaImage image = new();
        await image.WriteAsync(data, ct);
        return image;
    }

    // Mirrors the upstream `imageIntercept`: images are XORed against a repeating keystream
    // derived from the hex-encoded encryptionKey field on the page.
    private static byte[] Decrypt(byte[] data, string encryptionKeyHex)
    {
        byte[] keyStream = Convert.FromHexString(encryptionKeyHex);
        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
            result[i] = (byte)(data[i] ^ keyStream[i % keyStream.Length]);
        return result;
    }

    #endregion

    #region Data

    private sealed record Title(int TitleId, string Name, string? Author, string PortraitImageUrl, int Language);

    private sealed record AllTitlesGroupDto(List<Title> Titles);

    private sealed record AllTitlesViewDto(List<AllTitlesGroupDto> Groups);

    private sealed record ChapterDto(int ChapterId, string Name, string? SubTitle)
    {
        // The upstream source treats a chapter with no subtitle text as expired/inaccessible.
        public bool IsExpired => SubTitle is null;
    }

    private sealed record ChapterListGroupDto(List<ChapterDto> FirstChapterList, List<ChapterDto> LastChapterList);

    private sealed record TitleDetailViewDto(List<ChapterListGroupDto> ChapterListGroup)
    {
        public IEnumerable<ChapterDto> Chapters => ChapterListGroup.SelectMany(g => g.FirstChapterList.Concat(g.LastChapterList));
    }

    private sealed record MangaPageDto(string ImageUrl, string? EncryptionKey);

    private sealed record MangaPlusPageDto(MangaPageDto? MangaPage);

    private sealed record MangaViewerDto(List<MangaPlusPageDto> Pages, string? ViewToken);

    private sealed record SuccessResultDto(TitleDetailViewDto? TitleDetailView, MangaViewerDto? MangaViewer, AllTitlesViewDto? AllTitlesView);

    #endregion

    #region Protobuf

    private ref struct ProtoReader
    {
        private readonly ReadOnlySpan<byte> _data;
        private int _pos;

        public ProtoReader(ReadOnlySpan<byte> data)
        {
            _data = data;
            _pos = 0;
        }

        public bool HasMore => _pos < _data.Length;

        public (int Field, int WireType) ReadTag()
        {
            ulong tag = ReadVarint();
            return ((int)(tag >> 3), (int)(tag & 0x7));
        }

        public ulong ReadVarint()
        {
            ulong result = 0;
            int shift = 0;
            byte b;
            do
            {
                b = _data[_pos++];
                result |= (ulong)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return result;
        }

        public ReadOnlySpan<byte> ReadLengthDelimited()
        {
            int length = (int)ReadVarint();
            ReadOnlySpan<byte> slice = _data.Slice(_pos, length);
            _pos += length;
            return slice;
        }

        public void SkipField(int wireType)
        {
            switch (wireType)
            {
                case 0: ReadVarint(); break;
                case 1: _pos += 8; break;
                case 2:
                    // NB: must read the length into a local first - `_pos += (int)ReadVarint()` would
                    // read the stale pre-call `_pos` for the addition before ReadVarint's internal
                    // mutation of `_pos` (past the length-prefix bytes) takes effect.
                    int length = (int)ReadVarint();
                    _pos += length;
                    break;
                case 5: _pos += 4; break;
            }
        }
    }

    private static string Utf8(ReadOnlySpan<byte> bytes) => Encoding.UTF8.GetString(bytes);

    private static SuccessResultDto? ParseMangaPlusResponse(ReadOnlySpan<byte> data)
    {
        ProtoReader reader = new(data);
        while (reader.HasMore)
        {
            (int field, int wireType) = reader.ReadTag();
            if (field == 1)
                return ParseSuccessResult(reader.ReadLengthDelimited());
            reader.SkipField(wireType);
        }
        return null;
    }

    private static SuccessResultDto ParseSuccessResult(ReadOnlySpan<byte> data)
    {
        TitleDetailViewDto? titleDetailView = null;
        MangaViewerDto? mangaViewer = null;
        AllTitlesViewDto? allTitlesView = null;
        ProtoReader reader = new(data);
        while (reader.HasMore)
        {
            (int field, int wireType) = reader.ReadTag();
            switch (field)
            {
                case 8: titleDetailView = ParseTitleDetailView(reader.ReadLengthDelimited()); break;
                case 10: mangaViewer = ParseMangaViewer(reader.ReadLengthDelimited()); break;
                case 25: allTitlesView = ParseAllTitlesView(reader.ReadLengthDelimited()); break;
                default: reader.SkipField(wireType); break;
            }
        }
        return new SuccessResultDto(titleDetailView, mangaViewer, allTitlesView);
    }

    private static AllTitlesViewDto ParseAllTitlesView(ReadOnlySpan<byte> data)
    {
        List<AllTitlesGroupDto> groups = [];
        ProtoReader reader = new(data);
        while (reader.HasMore)
        {
            (int field, int wireType) = reader.ReadTag();
            if (field == 1)
                groups.Add(ParseAllTitlesGroup(reader.ReadLengthDelimited()));
            else
                reader.SkipField(wireType);
        }
        return new AllTitlesViewDto(groups);
    }

    private static AllTitlesGroupDto ParseAllTitlesGroup(ReadOnlySpan<byte> data)
    {
        List<Title> titles = [];
        ProtoReader reader = new(data);
        while (reader.HasMore)
        {
            (int field, int wireType) = reader.ReadTag();
            if (field == 2)
                titles.Add(ParseTitle(reader.ReadLengthDelimited()));
            else
                reader.SkipField(wireType);
        }
        return new AllTitlesGroupDto(titles);
    }

    private static Title ParseTitle(ReadOnlySpan<byte> data)
    {
        int titleId = 0;
        string name = "";
        string? author = null;
        string portraitImageUrl = "";
        int language = 0;
        ProtoReader reader = new(data);
        while (reader.HasMore)
        {
            (int field, int wireType) = reader.ReadTag();
            switch (field)
            {
                case 1: titleId = (int)reader.ReadVarint(); break;
                case 2: name = Utf8(reader.ReadLengthDelimited()); break;
                case 3: author = Utf8(reader.ReadLengthDelimited()); break;
                case 4: portraitImageUrl = Utf8(reader.ReadLengthDelimited()); break;
                case 7: language = (int)reader.ReadVarint(); break;
                default: reader.SkipField(wireType); break;
            }
        }
        return new Title(titleId, name, author, portraitImageUrl, language);
    }

    private static TitleDetailViewDto ParseTitleDetailView(ReadOnlySpan<byte> data)
    {
        List<ChapterListGroupDto> groups = [];
        ProtoReader reader = new(data);
        while (reader.HasMore)
        {
            (int field, int wireType) = reader.ReadTag();
            if (field == 28)
                groups.Add(ParseChapterListGroup(reader.ReadLengthDelimited()));
            else
                reader.SkipField(wireType);
        }
        return new TitleDetailViewDto(groups);
    }

    private static ChapterListGroupDto ParseChapterListGroup(ReadOnlySpan<byte> data)
    {
        List<ChapterDto> first = [];
        List<ChapterDto> last = [];
        ProtoReader reader = new(data);
        while (reader.HasMore)
        {
            (int field, int wireType) = reader.ReadTag();
            switch (field)
            {
                case 2: first.Add(ParseChapter(reader.ReadLengthDelimited())); break;
                case 4: last.Add(ParseChapter(reader.ReadLengthDelimited())); break;
                default: reader.SkipField(wireType); break;
            }
        }
        return new ChapterListGroupDto(first, last);
    }

    private static ChapterDto ParseChapter(ReadOnlySpan<byte> data)
    {
        int chapterId = 0;
        string name = "";
        string? subTitle = null;
        ProtoReader reader = new(data);
        while (reader.HasMore)
        {
            (int field, int wireType) = reader.ReadTag();
            switch (field)
            {
                case 2: chapterId = (int)reader.ReadVarint(); break;
                case 3: name = Utf8(reader.ReadLengthDelimited()); break;
                case 4: subTitle = Utf8(reader.ReadLengthDelimited()); break;
                default: reader.SkipField(wireType); break;
            }
        }
        return new ChapterDto(chapterId, name, subTitle);
    }

    private static MangaViewerDto ParseMangaViewer(ReadOnlySpan<byte> data)
    {
        List<MangaPlusPageDto> pages = [];
        string? viewToken = null;
        ProtoReader reader = new(data);
        while (reader.HasMore)
        {
            (int field, int wireType) = reader.ReadTag();
            switch (field)
            {
                case 1: pages.Add(ParseMangaPlusPage(reader.ReadLengthDelimited())); break;
                case 19: viewToken = Utf8(reader.ReadLengthDelimited()); break;
                default: reader.SkipField(wireType); break;
            }
        }
        return new MangaViewerDto(pages, viewToken);
    }

    private static MangaPlusPageDto ParseMangaPlusPage(ReadOnlySpan<byte> data)
    {
        MangaPageDto? mangaPage = null;
        ProtoReader reader = new(data);
        while (reader.HasMore)
        {
            (int field, int wireType) = reader.ReadTag();
            if (field == 1)
                mangaPage = ParseMangaPage(reader.ReadLengthDelimited());
            else
                reader.SkipField(wireType);
        }
        return new MangaPlusPageDto(mangaPage);
    }

    private static MangaPageDto ParseMangaPage(ReadOnlySpan<byte> data)
    {
        string imageUrl = "";
        string? encryptionKey = null;
        ProtoReader reader = new(data);
        while (reader.HasMore)
        {
            (int field, int wireType) = reader.ReadTag();
            switch (field)
            {
                case 1: imageUrl = Utf8(reader.ReadLengthDelimited()); break;
                case 5: encryptionKey = Utf8(reader.ReadLengthDelimited()); break;
                default: reader.SkipField(wireType); break;
            }
        }
        return new MangaPageDto(imageUrl, encryptionKey);
    }

    #endregion
}

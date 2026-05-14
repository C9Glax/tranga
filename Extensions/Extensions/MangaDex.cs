using System.Threading.RateLimiting;
using Common.Datatypes;
using Common.Helpers;
using Common.Settings;
using Extensions.Data;
using Newtonsoft.Json.Linq;
using GeneratedExtensionClients.GeneratedClients.MangaDex;
using Manga = GeneratedExtensionClients.GeneratedClients.MangaDex.Manga;

namespace Extensions.Extensions;

public sealed class MangaDex : IDownloadExtension, IMetadataExtension
{
    public Guid Identifier { get; init; } = Guid.Parse("019ce521-deaf-7739-9e14-eb6f4afc86e2");

    public string Name { get; init; } = "MangaDex";

    public Language[] SupportedLanguages { get; init; } = ["en-us"!];

    // ReSharper disable once ValueParameterNotUsed
    public string BaseUrl
    {
        get => Client.BaseUrl;
        init => Client.BaseUrl = "https://api.mangadex.org/";
    }

    private static readonly RequestClient MangaDexRequestClient = new(new SlidingWindowRateLimiter(
        new SlidingWindowRateLimiterOptions()
        {
            AutoReplenishment = true,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1,
            PermitLimit = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));

    // ReSharper disable once InconsistentNaming
    private readonly MangaDexApiClient Client = new(MangaDexRequestClient);

    #region Search

    public async Task<List<MangaInfo>?> SearchDownload(SearchQuery query, CancellationToken ct)
    {
        if (await GetManga(query, ct) is not { } mangas)
            return null;

        List<Task<MangaInfo?>> tasks = mangas.Select(manga => ParseMangaInfo(manga, query.Language, ct)).ToList();
        await Task.WhenAll(tasks);

        List<MangaInfo> results = [];
        foreach (Task<MangaInfo?> task in tasks)
        {
            if (task is { IsCompletedSuccessfully: true, Result: { } parsed } &&
                (Settings.AllowNSFW || parsed.NSFW != true))
                results.Add(parsed);
        }

        return results;
    }

    private async Task<MangaInfo?> ParseMangaInfo(Manga manga, Language? language, CancellationToken ct)
    {
        if (manga.Id is not { } id)
            return null;
        if (GetLocalizedString(manga.Attributes?.Title, language) is not { } title)
            return null;
        if (await GetCover(manga, ct) is not { } cover)
            return null;
        string url = $"https://mangadex.org/title/{id}";
        return new MangaInfo(this.Identifier, title, url, id.ToString(), cover,
            GetLocalizedString(manga.Attributes?.Description, language),
            manga.Attributes?.ContentRating is { } rating && rating != MangaAttributesContentRating.Safe);
    }

    #endregion

    #region Chapters

    public async Task<List<ChapterInfo>?> GetChapters(MangaInfo mangaInfo, CancellationToken ct)
    {
        int offset = 0;
        int total = 0;
        const int limit = 100;
        List<Chapter> chapters = [];
        do
        {
            ChapterList list = await Client.GetChapterAsync(
                manga: Guid.Parse(mangaInfo.Identifier),
                offset: offset,
                limit: limit,
                // https://api.mangadex.org/docs/3-enumerations/#language-codes--localization
                translatedLanguage: [Settings.DownloadLanguage.TwoLetterISOLanguageName],
                cancellationToken: ct);

            if (list.Data is null)
                return null;

            chapters.AddRange(list.Data);

            total = list.Total ?? 0;
            offset += limit;
        } while (offset < total);


        return ParseChaptersResult(chapters.ToArray());
    }

    private List<ChapterInfo> ParseChaptersResult(Chapter[] chapters)
    {
        List<ChapterInfo> result = new();
        foreach (Chapter chapter in chapters)
        {
            if (chapter.Attributes?.ExternalUrl is not null)
                continue; // Skip chapters from external providers
            if (chapter.Id is not { } id)
                continue;
            if (chapter.Attributes?.Chapter is not { } number)
                continue;
            string url = $"https://mangadex.org/chapter/{id}";
            result.Add(new ChapterInfo(this.Identifier, number, url, id.ToString(), chapter.Attributes?.Volume,
                chapter.Attributes?.Title));
        }

        return result;
    }

    #endregion

    #region Images

    public async Task<List<ChapterImage>?> FetchChapterImages(ChapterInfo chapterInfo, CancellationToken ct)
    {
        Response11 r = await Client.GetAtHomeServerChapterIdAsync(Guid.Parse(chapterInfo.Identifier),
            cancellationToken: ct);
        if (r.Chapter?.Data is null)
            return null;
        List<string> urls = r.Chapter.Data.Select(image => $"{r.BaseUrl}/data/{r.Chapter.Hash}/{image}").ToList();

        List<ChapterImage> images = new();
        List<(int index, Task<HttpResponseMessage> request)> requests = urls
            .Select((url, index) => (index, MangaDexRequestClient.GetAsync(url, ct)))
            .ToList();

        await Task.WhenAll(requests.Select(t => t.request));

        if (requests.Any(tuple => !tuple.request.IsCompletedSuccessfully))
            return null;

        foreach ((int index, Task<HttpResponseMessage> request) in requests)
        {
            TrangaImage image = new();
            Stream data = await request.Result.Content.ReadAsStreamAsync(ct);
            await data.CopyToAsync(image, ct);
            images.Add(new ChapterImage(this.Identifier, chapterInfo.Identifier, index, image));
        }

        return images;
    }

    #endregion

    #region SearchMetadata

    public async Task<List<SearchResult>?> SearchMetadata(SearchQuery searchQuery, CancellationToken ct)
    {
        if (await GetManga(searchQuery, ct) is not { } mangas)
            return null;

        List<Task<SearchResult?>> tasks = mangas.Select(m => ParseSearchResult(m, searchQuery.Language, ct)).ToList();
        await Task.WhenAll(tasks);

        List<SearchResult> results = [];
        foreach (Task<SearchResult?> task in tasks)
        {
            if (task is { IsCompletedSuccessfully: true, Result: { } parsed } &&
                (Settings.AllowNSFW || parsed.NSFW != true))
                results.Add(parsed);
        }

        return results;
    }


    private async Task<SearchResult?> ParseSearchResult(Manga manga, Language? language, CancellationToken ct)
    {
        if (manga.Id is not { } id)
            return null;
        if (GetLocalizedString(manga.Attributes?.Title, language) is not { } seriesTitle)
            return null;
        if (await GetCover(manga, ct) is not { } cover)
            return null;
        string url = $"https://mangadex.org/title/{id}";
        ReleaseStatus? status = manga.Attributes?.Status switch
        {
            MangaAttributesStatus.Completed => ReleaseStatus.Complete,
            MangaAttributesStatus.Ongoing => ReleaseStatus.Ongoing,
            MangaAttributesStatus.Cancelled => ReleaseStatus.Cancelled,
            MangaAttributesStatus.Hiatus => ReleaseStatus.Hiatus,
            _ => null
        };
        string[]? authors = manga.Relationships?.Where(r => r is { Type: "author", Attributes: AuthorAttributes })
            .Select(r =>
            {
                AuthorAttributes? a = r.Attributes as AuthorAttributes;
                return a?.Name!;
            }).ToArray();
        return new SearchResult()
        {
            MetadataExtensionIdentifier = this.Identifier,
            Identifier = id.ToString(),
            Series = seriesTitle,
            Summary = GetLocalizedString(manga.Attributes?.Description, language),
            Cover = cover,
            Year = manga.Attributes?.Year,
            Url = url,
            Status = status,
            Language = manga.Attributes?.OriginalLanguage,
            Genres = manga.Attributes?.Tags?.Select(t => GetLocalizedString(t.Attributes?.Name, language))
                .Where(t => t is not null).Select(t => t!).ToArray(),
            Authors = authors,
            NSFW = manga.Attributes?.ContentRating is { } rating && rating != MangaAttributesContentRating.Safe
        };
    }

    #endregion

    private async Task<Manga[]?> GetManga(SearchQuery searchQuery, CancellationToken ct)
    {
        Anonymous4[] includes =
        [
            Anonymous4.Manga,
            Anonymous4.Cover_art,
            Anonymous4.Author,
            Anonymous4.Artist,
            Anonymous4.Tag
        ];
        if (searchQuery.MangaDexSeriesId is { } seriesId && await Client.GetMangaIdAsync(seriesId, includes, ct) is
                { Result: MangaResponseResult.Ok, Data: { } manga })
        {
            return [manga];
        }

        if (await Client.GetSearchMangaAsync(limit: 10, offset: 0, title: searchQuery.Title, includes: includes,
                cancellationToken: ct) is not { Result: "ok", Data: { } mangas })
            return null;

        return mangas.ToArray();
    }

    private async Task<TrangaImage?> GetCover(Manga manga, CancellationToken ct)
    {
        if (manga.Id is not { } id)
            return null;
        if (manga.Relationships?.FirstOrDefault(r => r.Type == "cover_art") is not { Attributes: JObject attributes } ||
            attributes.ToObject<CoverAttributes>() is not { } coverAttributes)
            return null;
        if (coverAttributes.FileName is not { } fileName)
            return null;
        Uri requestUri = new($"https://uploads.mangadex.org/covers/{id}/{fileName}");
        if (await MangaDexRequestClient.GetAsync(requestUri, ct) is not { IsSuccessStatusCode: true } response)
            return null;
        TrangaImage image = new();
        Stream data = await response.Content.ReadAsStreamAsync(ct);
        await data.CopyToAsync(image, ct);
        return image;
    }


    private string? GetLocalizedString(LocalizedString? str, Language? language)
    {
        if (str is null)
            return null;
        if (language is not null && str.TryGetValue(language.Name, out string? lang))
            return lang;
        if (str.FirstOrDefault(kv => kv.Key.Equals("en-us", StringComparison.InvariantCultureIgnoreCase)) is
            { Value: { } langEnUs }) return langEnUs;
        return str.FirstOrDefault().Value;
    }
}
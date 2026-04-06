using Common.Datatypes;
using Common.Helpers;
using Extensions.Data;
using Newtonsoft.Json.Linq;
using NSwagClients.GeneratedClients.MangaDex;
using Manga = NSwagClients.GeneratedClients.MangaDex.Manga;

namespace Extensions.Extensions;

public sealed class MangaDex : IDownloadExtension, IMetadataExtension
{
    public Guid Identifier { get; init; } = Guid.Parse("019ce521-deaf-7739-9e14-eb6f4afc86e2");

    public string Name { get; init; } = "MangaDex";

    public Language[] SupportedLanguages { get; init; } = ["en-us"!];
    
    // ReSharper disable once ValueParameterNotUsed
    public string BaseUrl { get => Client.BaseUrl; init => Client.BaseUrl = "https://api.mangadex.org/"; }

    // ReSharper disable once InconsistentNaming
    private readonly MangaDexApiClient Client = new(new RequestClient());

    #region Search
    public async Task<List<MangaInfo>?> SearchDownload(SearchQuery query, CancellationToken ct)
    {
        if (await GetManga(query, ct) is not { } mangas)
            return null;

        List<MangaInfo> results = [];
        foreach (Manga m in mangas)
        {
            if(await ParseMangaInfo(m, query.Language, ct) is { } parsed&& (Settings.Settings.AllowNSFW || parsed.NSFW != true))
                results.Add(parsed);
        }
        return results;
    }

    private async Task<MangaInfo?> ParseMangaInfo(Manga manga, Language? language, CancellationToken ct)
    {
        if(manga.Id is not { } id)
            return null;
        if(GetLocalizedString(manga.Attributes?.Title, language) is not { } title)
            return null;
        if(await GetCover(manga, ct) is not { } cover)
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
        List<Chapter> chapters = new();
        do
        {
            ChapterList list = await Client.GetChapterAsync(manga: Guid.Parse(mangaInfo.Identifier), offset: offset,
                limit: limit, cancellationToken: ct);
            
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
            if(chapter.Attributes?.ExternalUrl is not null)
                continue; // Skip chapters from external providers
            if(chapter.Id is not { } id)
                continue;
            if(chapter.Attributes?.Chapter is not { } number)
                continue;
            string url = $"https://mangadex.org/chapter/{id}";
            result.Add(new ChapterInfo(this.Identifier, number, url, id.ToString(), chapter.Attributes?.Volume, chapter.Attributes?.Title));
        }
        return result;
    }
    #endregion

    #region Images
    public async Task<List<ChapterImage>?> GetChapterImages(ChapterInfo chapterInfo, CancellationToken ct)
    {
        Response11 r = await Client.GetAtHomeServerChapterIdAsync(Guid.Parse(chapterInfo.Identifier), cancellationToken: ct);
        if (r.Chapter?.Data is null)
            return null;
        List<string> urls = r.Chapter.Data.Select(image => $"{r.BaseUrl}/data/{r.Chapter.Hash}/{image}").ToList();

        List<ChapterImage> images = new();
        RequestClient client = new RequestClient();
        for (int i = 0; i < urls.Count; i++)
        {
            if (await client.GetAsync(urls[i], ct) is not { IsSuccessStatusCode: true } response)
                return null;
            MemoryStream memoryStream = new ();
            Stream data = await response.Content.ReadAsStreamAsync(ct);
            await data.CopyToAsync(memoryStream, ct);
            images.Add(new ChapterImage(this.Identifier, chapterInfo.Identifier, i, memoryStream));
        }

        return images;
    }
    #endregion

    #region SearchMetadata

    public async Task<List<SearchResult>?> SearchMetadata(SearchQuery searchQuery, CancellationToken ct)
    {
        if (await GetManga(searchQuery, ct) is not { } mangas)
            return null;

        List<SearchResult> results = [];
        foreach (Manga m in mangas)
        {
            if(await ParseSearchResult(m, searchQuery.Language, ct) is { } parsed&& (Settings.Settings.AllowNSFW || parsed.NSFW != true))
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
        if(await GetCover(manga, ct) is not { } cover)
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
        string[]? authors = manga.Relationships?.Where(r => r is { Type: "author", Attributes: AuthorAttributes }).Select(r =>
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
            Genres = manga.Attributes?.Tags?.Select(t => GetLocalizedString(t.Attributes?.Name, language)).Where(t => t is not null).Select(t => t!).ToArray(),
            Authors = authors,
            NSFW = manga.Attributes?.ContentRating is { } rating && rating != MangaAttributesContentRating.Safe
        };
    }
    #endregion

    private async Task<Manga[]?> GetManga(SearchQuery searchQuery, CancellationToken ct)
    {
        Anonymous4[] includes = [
            Anonymous4.Manga,
            Anonymous4.Cover_art,
            Anonymous4.Author,
            Anonymous4.Artist,
            Anonymous4.Tag
        ];
        if (searchQuery.MangaDexSeriesId is { } seriesId && await Client.GetMangaIdAsync(seriesId, includes, ct) is { Result: MangaResponseResult.Ok, Data: { } manga })
        {
            return [manga];
        }

        if (await Client.GetSearchMangaAsync(limit: 10, offset: 0, title: searchQuery.Title, includes: includes, cancellationToken: ct) is not { Result: "ok", Data: { } mangas })
            return null;

        return mangas.ToArray();
    }

    private async Task<MemoryStream?> GetCover(Manga manga, CancellationToken ct)
    {
        if (manga.Id is not { } id)
            return null;
        if (manga.Relationships?.FirstOrDefault(r => r.Type == "cover_art") is not { Attributes: JObject attributes } || attributes.ToObject<CoverAttributes>() is not { } coverAttributes)
            return null;
        if(coverAttributes.FileName is not { } fileName)
            return null;
        Uri requestUri = new($"https://uploads.mangadex.org/covers/{id}/{fileName}");
        if (await new RequestClient().GetAsync(requestUri, ct) is not { IsSuccessStatusCode: true } response)
            return null;
        MemoryStream memoryStream = new ();
        Stream data = await response.Content.ReadAsStreamAsync(ct);
        await data.CopyToAsync(memoryStream, ct);
        return memoryStream;
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
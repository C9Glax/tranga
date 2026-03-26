using Common.Datatypes;
using Common.Helpers;
using DownloadExtensions.Data;
using Newtonsoft.Json.Linq;
using NSwagClients.GeneratedClients.MangaDex;
using Manga = NSwagClients.GeneratedClients.MangaDex.Manga;

namespace DownloadExtensions.Extensions;

public sealed class MangaDex : IDownloadExtension
{
    public Guid Identifier { get; init; } = Guid.Parse("019ce521-deaf-7739-9e14-eb6f4afc86e2");

    public string Name { get; init; } = "MangaDex";
    
    public Language[] SupportedLanguages { get; init; } = ["en-us"];
    
    // ReSharper disable once ValueParameterNotUsed
    public string BaseUrl { get => Client.BaseUrl; init => Client.BaseUrl = "https://api.mangadex.org/"; }

    // ReSharper disable once InconsistentNaming
    private readonly MangaDexApiClient Client = new(new RequestClient());

    #region Search
    public async Task<List<MangaInfo>?> Search(SearchQuery query, CancellationToken ct)
    {
        MangaList list = await Client.GetSearchMangaAsync(
            includes: [Anonymous4.Cover_art],
            title: query.Title,
            availableTranslatedLanguage: query.Language is null ? null : [query.Language],
            year: query.Year,
            cancellationToken: ct
        );

        if (list.Data is null)
            return null;
        
        return await ParseSearchResult(list.Data.ToArray(), query.Language, ct);
    }

    private async Task<List<MangaInfo>> ParseSearchResult(Manga[] mangas, Language? language, CancellationToken ct)
    {
        List<MangaInfo> result = new();
        foreach (Manga manga in mangas)
        {
            if(manga.Id is not { } id)
                continue;
            if(manga.Attributes?.Title?.GetLocalizedString(language) is not { } title)
                continue;
            if(await GetCover(manga, ct) is not { } cover)
                continue;
            string url = $"https://mangadex.org/title/{id}";
            result.Add(new MangaInfo(this.Identifier, title, url, id.ToString(), cover, manga.Attributes.Description?.GetLocalizedString(language)));
        }
        return result;
    }

    private async Task<MemoryStream?> GetCover(Manga manga, CancellationToken ct)
    {
        if (manga.Id is not { } id)
            return null;
        if (manga.Relationships?.FirstOrDefault(r => r.Type == "cover_art") is not { Attributes: JObject attributes })
            return null;
        if(attributes["fileName"]?.Value<string>() is not { } fileName)
            return null;
        Uri requestUri = new($"https://uploads.mangadex.org/covers/{id}/{fileName}");
        if (await new RequestClient().GetAsync(requestUri, ct) is not { IsSuccessStatusCode: true } response)
            return null;
        MemoryStream memoryStream = new ();
        Stream data = await response.Content.ReadAsStreamAsync(ct);
        await data.CopyToAsync(memoryStream, ct);
        return memoryStream;
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
}

internal static class Helper
{
    public static string? GetLocalizedString(this LocalizedString str, Language? language)
    {
        if (language is not null && str.TryGetValue(language, out string? lang))
            return lang;
        if (str.FirstOrDefault(kv => kv.Key.Equals("en-us", StringComparison.InvariantCultureIgnoreCase)) is
            { Value: { } langEnUs }) return langEnUs;
        return str.FirstOrDefault().Value;
    }
}
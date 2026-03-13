using System.Text.Json.Nodes;
using Common.Data;
using DownloadExtensions.Data;
using DownloadExtensions.Extensions.MangaDex.DTOs;
using DownloadExtensions.Helpers;

namespace DownloadExtensions.Extensions.MangaDex;

public sealed class MangaDex : IDownloadExtension<MangaDex>
{
    public Guid Identifier { get; init; } = Guid.Parse("019ce521-deaf-7739-9e14-eb6f4afc86e2");

    public string Name { get; init; } = "MangaDex";
    
    public Language[] SupportedLanguages { get; init; } = ["en-us"];
    
    public string BaseUrl { get; init; } = new ("https://api.mangadex.org/");

    private readonly RequestClient _client = new ();

    #region Search
    public async Task<MangaSearchResult<MangaDex>?> Search(SearchQuery query, CancellationToken ct)
    {
        HttpRequestMessage request = CreateSearchRequest(query);
        MangaDexSearchResultDTO? result = await _client.SendAsyncAndParseJson<MangaDexSearchResultDTO>(request, ct);
        if (result is null || result.Result != "ok")
            return null;
        return await ParseSearchResult(result.Data, ct);
    }

    private HttpRequestMessage CreateSearchRequest(SearchQuery query)
    {
        UriBuilder uriBuilder = new(BaseUrl)
        {
            Path = "/manga"
        };

        uriBuilder.AddQueryParameter("includes[]", "cover_art");
        if (!string.IsNullOrEmpty(query.Title))
            uriBuilder.AddQueryParameter("title", query.Title);
        if (query.Tags is { Length: > 0 }) ; // TODO: Tags are referenced by UUIDs
        if (!string.IsNullOrEmpty(query.Artist)) ; // TODO: Artists are referenced by UUIDs
        if (!string.IsNullOrEmpty(query.Author)) ; // TODO: Authors are referenced by UUIDs
        if (query.Language is { } language)
            uriBuilder.AddQueryParameter("originalLanguage[]", language);
        if (query.Year is { } year)
            uriBuilder.AddQueryParameter("year", year.ToString());
        if (query.ContentRating is { } contentRating)
            uriBuilder.AddQueryParameter("contentRating[]", contentRating.ToString());
        
        HttpRequestMessage message = new(HttpMethod.Get, uriBuilder.Uri);
        return message;
    }

    private async Task<MangaSearchResult<MangaDex>> ParseSearchResult(MangaDexMangaDTO[] mangas, CancellationToken ct)
    {
        MangaSearchResult<MangaDex> result = new();
        foreach (MangaDexMangaDTO manga in mangas)
        {
            if(manga.Attributes.GetAttribute<string>("title") is not { } title)
                continue;
            if(manga.Attributes.GetAttribute<string>("description") is not { } description)
                continue;
            if(await GetCover(manga, ct) is not { } cover)
                continue;
            string url = $"https://mangadex.org/title/{manga.Id}";
            string identifier = manga.Id.ToString();
            result.Add(new MangaInfo<MangaDex>(title, url, identifier, cover, description));
        }
        return result;
    }

    private async Task<MemoryStream?> GetCover(MangaDexMangaDTO manga, CancellationToken ct)
    {
        if (manga.Relationships.FirstOrDefault(r => r.Type == "cover_art") is not { Attributes: { } attributes })
            return null;
        if(!attributes.TryGetValue("fileName", out JsonNode? node) || node.GetValue<string>() is not { } fileName)
            return null;
        Uri requestUri = new($"https://uploads.mangadex.org/covers/{manga.Id}/{fileName}");
        if (await _client.GetAsync(requestUri, ct) is not { IsSuccessStatusCode: true } response)
            return null;
        MemoryStream memoryStream = new ();
        Stream data = await response.Content.ReadAsStreamAsync(ct);
        await data.CopyToAsync(memoryStream, ct);
        return memoryStream;
    }
    #endregion

    #region Chapters
    public async Task<List<ChapterInfo<MangaDex>>?> GetChapters(MangaInfo<MangaDex> mangaInfo, CancellationToken ct)
    {
        HttpRequestMessage request = CreateChaptersRequest(mangaInfo);
        MangaDexChapterResultDTO? result = await _client.SendAsyncAndParseJson<MangaDexChapterResultDTO>(request, ct);
        if (result is null || result.Result != "ok")
            return null;
        return ParseChaptersResult(result.Data);
    }
    
    private HttpRequestMessage CreateChaptersRequest(MangaInfo<MangaDex> mangaInfo)
    {
        UriBuilder uriBuilder = new(BaseUrl)
        {
            Path = "/chapter"
        };
        uriBuilder.AddQueryParameter("manga", mangaInfo.Identifier);
        
        HttpRequestMessage message = new(HttpMethod.Get, uriBuilder.Uri);
        return message;
    }
    
    private List<ChapterInfo<MangaDex>> ParseChaptersResult(MangaDexChapterDTO[] chapters)
    {
        List<ChapterInfo<MangaDex>> result = new();
        foreach (MangaDexChapterDTO chapter in chapters)
        {
            if(chapter.Attributes.ExternalUrl is not null)
                continue; // Skip chapters from external providers
            string number = chapter.Attributes.Chapter;
            string url = $"https://mangadex.org/chapter/{chapter.Id}";
            string identifier = chapter.Id.ToString();
            string? volume = chapter.Attributes.Volume;
            string? title = chapter.Attributes.Title;
            result.Add(new ChapterInfo<MangaDex>(number, url, identifier, volume, title));
        }
        return result;
    }
    #endregion

    #region Images
    public async Task<List<ChapterImage<MangaDex>>?> GetChapterImages(ChapterInfo<MangaDex> chapterInfo, CancellationToken ct)
    {
        if (await GetChapterImageUrls(chapterInfo, ct) is not { } urls)
            return null;

        List<ChapterImage<MangaDex>> images = new();
        for (int i = 0; i < urls.Count; i++)
        {
            if (await _client.GetAsync(urls[i], ct) is not { IsSuccessStatusCode: true } response)
                return null;
            MemoryStream memoryStream = new ();
            Stream data = await response.Content.ReadAsStreamAsync(ct);
            await data.CopyToAsync(memoryStream, ct);
            images.Add(new ChapterImage<MangaDex>(chapterInfo.Identifier, i, memoryStream));
        }

        return images;
    }

    private async Task<List<string>?> GetChapterImageUrls(ChapterInfo<MangaDex> chapterInfo, CancellationToken ct)
    {
        UriBuilder uriBuilder = new(BaseUrl)
        {
            Path = $"/at-home/server/{chapterInfo.Identifier}"
        };
        HttpRequestMessage message = new(HttpMethod.Get, uriBuilder.Uri);
        if (await _client.SendAsyncAndParseJson<MangaDexAtHomeResultDTO>(message, ct) is not { } response)
            return null;

        return response.Chapter.Data.Select(image => $"{response.BaseUrl}/data/{response.Chapter.Hash}/{image}").ToList();
    }
    #endregion
}
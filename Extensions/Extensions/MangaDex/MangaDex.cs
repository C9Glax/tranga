using System.Text.Json.Nodes;
using Extensions.Data;
using Extensions.Extensions.MangaDex.DTOs;
using Extensions.Helpers;

namespace Extensions.Extensions.MangaDex;

public sealed class MangaDex : IExtension<MangaDex>
{
    public Language[] SupportedLanguages { get; init; } = ["en-us"];
    
    public string BaseUrl { get; init; } = new ("https://api.mangadex.org/");

    private sealed record ChapterIdentifier(Guid Value) : IChapterIdentifier<MangaDex>;

    private sealed record MangaIdentifier(Guid Value) : IMangaIdentifier<MangaDex>;

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
            MangaIdentifier identifier = new(manga.Id);
            result.Add(new Manga<MangaDex>(title, url, identifier, cover, description));
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
    public async Task<List<Chapter<MangaDex>>?> GetChapters(Manga<MangaDex> manga, CancellationToken ct)
    {
        HttpRequestMessage request = CreateChaptersRequest(manga);
        MangaDexChapterResultDTO? result = await _client.SendAsyncAndParseJson<MangaDexChapterResultDTO>(request, ct);
        if (result is null || result.Result != "ok")
            return null;
        return ParseChaptersResult(result.Data);
    }
    
    private HttpRequestMessage CreateChaptersRequest(Manga<MangaDex> manga)
    {
        UriBuilder uriBuilder = new(BaseUrl)
        {
            Path = "/chapter"
        };
        uriBuilder.AddQueryParameter("manga", (manga.Identifier as MangaIdentifier)!.Value.ToString());
        
        HttpRequestMessage message = new(HttpMethod.Get, uriBuilder.Uri);
        return message;
    }
    
    private List<Chapter<MangaDex>> ParseChaptersResult(MangaDexChapterDTO[] chapters)
    {
        List<Chapter<MangaDex>> result = new();
        foreach (MangaDexChapterDTO chapter in chapters)
        {
            if(chapter.Attributes.ExternalUrl is not null)
                continue; // Skip chapters from external providers
            string number = chapter.Attributes.Chapter;
            string url = $"https://mangadex.org/chapter/{chapter.Id}";
            ChapterIdentifier identifier = new(chapter.Id);
            string? volume = chapter.Attributes.Volume;
            string? title = chapter.Attributes.Title;
            result.Add(new Chapter<MangaDex>(number, url, identifier, volume, title));
        }
        return result;
    }
    #endregion

    #region Images
    public async Task<List<ChapterImage<MangaDex>>?> GetChapterImages(Chapter<MangaDex> chapter, CancellationToken ct)
    {
        if (await GetChapterImageUrls(chapter, ct) is not { } urls)
            return null;

        List<ChapterImage<MangaDex>> images = new();
        for (int i = 0; i < urls.Count; i++)
        {
            if (await _client.GetAsync(urls[i], ct) is not { IsSuccessStatusCode: true } response)
                return null;
            MemoryStream memoryStream = new ();
            Stream data = await response.Content.ReadAsStreamAsync(ct);
            await data.CopyToAsync(memoryStream, ct);
            images.Add(new ChapterImage<MangaDex>(chapter.Identifier, i, memoryStream));
        }

        return images;
    }

    private async Task<List<string>?> GetChapterImageUrls(Chapter<MangaDex> chapter, CancellationToken ct)
    {
        UriBuilder uriBuilder = new(BaseUrl)
        {
            Path = $"/at-home/server/{(chapter.Identifier as ChapterIdentifier)!.Value.ToString()}"
        };
        HttpRequestMessage message = new(HttpMethod.Get, uriBuilder.Uri);
        if (await _client.SendAsyncAndParseJson<MangaDexAtHomeResultDTO>(message, ct) is not { } response)
            return null;

        return response.Chapter.Data.Select(image => $"{response.BaseUrl}/data/{response.Chapter.Hash}/{image}").ToList();
    }
    #endregion
}
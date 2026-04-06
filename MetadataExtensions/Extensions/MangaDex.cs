using Common.Datatypes;
using Common.Helpers;
using Newtonsoft.Json.Linq;
using NSwagClients.GeneratedClients.MangaDex;
using Manga = NSwagClients.GeneratedClients.MangaDex.Manga;

namespace MetadataExtensions.Extensions;

public sealed class MangaDex : IMetadataExtension
{
    public Guid Identifier { get; init; } = Guid.Parse("019d6340-9787-79fc-82cb-4dae3383e8af");
    // ReSharper disable once ValueParameterNotUsed
    public string BaseUrl { get => Client.BaseUrl; init => Client.BaseUrl = "https://api.mangadex.org/"; }
    public string Name { get; init; } = "MangaDex";
    // ReSharper disable once InconsistentNaming
    private readonly MangaDexApiClient Client = new(new RequestClient());
    
    public async Task<List<SearchResult>?> Search(SearchQuery searchQuery, CancellationToken ct)
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
            if(await ParseSearchResult(manga, ct) is { } parsed)
                return [parsed];
            else return null;
        }

        if (await Client.GetSearchMangaAsync(limit: 10, offset: 0, title: searchQuery.Title, includes: includes, cancellationToken: ct) is not { Result: "ok", Data: { } mangas })
            return null;

        List<SearchResult> results = [];
        foreach (Manga m in mangas)
        {
            if(await ParseSearchResult(m, ct) is { } parsed)
                results.Add(parsed);
        }
        return results;
    }
    
    private async Task<MemoryStream?> GetCover(Guid mangaId, Relationship? relationship, CancellationToken ct)
    {
        if (relationship is null)
            return null;
        if (relationship.Type != "cover_art")
            return null;
        if ((relationship.Attributes as JObject)?.ToObject<CoverAttributes>() is not { } attributes )
            return null;
        if(attributes.FileName is not { } fileName)
            return null;
        Uri requestUri = new($"https://uploads.mangadex.org/covers/{mangaId}/{fileName}");
        if (await new RequestClient().GetAsync(requestUri, ct) is not { IsSuccessStatusCode: true } response)
            return null;
        MemoryStream memoryStream = new ();
        Stream data = await response.Content.ReadAsStreamAsync(ct);
        await data.CopyToAsync(memoryStream, ct);
        return memoryStream;
    }
    
    private async Task<SearchResult?> ParseSearchResult(Manga manga, CancellationToken ct)
    {
        if (manga.Id is not { } id)
            return null;
        if (manga.Attributes?.Title?.GetLocalizedString("en-us") is not { } seriesTitle)
            return null;
        if(await GetCover(id, manga.Relationships?.FirstOrDefault(r => r.Type == "cover_art"), ct) is not { } cover)
            return null;
        string url = $"https://mangadex.org/title/{id}";
        ReleaseStatus? status = manga.Attributes.Status switch
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
            Summary = manga.Attributes.Description?.GetLocalizedString("en-us"),
            Cover = cover,
            Year = manga.Attributes.Year,
            Url = url,
            Status = status,
            Language = manga.Attributes.OriginalLanguage,
            Genres = manga.Attributes.Tags?.Select(t => t.Attributes?.Name?.GetLocalizedString("en-us")).Where(t => t is not null).Select(t => t!).ToArray(),
            Authors = authors
        };
    }
}

internal static class Helper
{
    public static string? GetLocalizedString(this LocalizedString str, Language? language)
    {
        if (language is not null && str.TryGetValue(language.Name, out string? lang))
            return lang;
        if (str.FirstOrDefault(kv => kv.Key.Equals("en-us", StringComparison.InvariantCultureIgnoreCase)) is
            { Value: { } langEnUs }) return langEnUs;
        return str.FirstOrDefault().Value;
    }
}
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Common.Datatypes;
using Common.Helpers;
using Common.Settings;
using Extensions.Data;

namespace Extensions.Extensions;

/// <summary>
/// Metadata from https://myanimelist.net/ via the official API v2. Every request needs a client-id, obtained by
/// registering an application at https://myanimelist.net/apiconfig and passed in through the <c>MAL_CLIENT_ID</c>
/// environment variable; without it the API answers 403, so this extension reports no results at all and the search
/// silently falls back to the other metadata providers.
/// </summary>
public sealed class MyAnimeList : IMetadataExtension
{
    public Guid Identifier { get; init; } = Guid.Parse("69ade113-7c3c-4ef8-a575-e5082edb5585");

    public string Name { get; init; } = "MyAnimeList";

    public string BaseUrl { get; init; } = "https://myanimelist.net";

    public string IconUrl { get; init; } = "https://cdn.myanimelist.net/img/sp/icon/apple-touch-icon-256.png";

    private const string ApiUrl = "https://api.myanimelist.net/v2";

    private const string Fields =
        "id,title,main_picture,alternative_titles,start_date,synopsis,genres,authors{first_name,last_name},status,nsfw";

    /// <summary>MyAnimeList rejects <c>q</c> shorter than this.</summary>
    private const int MinimumQueryLength = 3;

    // MyAnimeList publishes no rate-limit figure and throttles aggressively; one request per second is the same
    // budget MangaPlus uses and has never tripped it.
    private static readonly RequestClient MyAnimeListRequestClient = new(new SlidingWindowRateLimiter(
        new SlidingWindowRateLimiterOptions()
        {
            AutoReplenishment = true,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1,
            PermitLimit = 1,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));

    public async Task<List<SearchResult>?> SearchMetadata(SearchQuery searchQuery, CancellationToken ct)
    {
        if (EnvVars.MAL_CLIENT_ID is null)
            return null;

        // If a MyAnimeList ID is included, get the manga directly instead of running a fuzzy title search
        if (searchQuery.MyAnimeListSeriesId is { } id)
        {
            MyAnimeListManga? manga = await Get<MyAnimeListManga>($"{ApiUrl}/manga/{id}?fields={Fields}", ct);
            if (manga is null)
                return null;
            if (await ParseSearchResult(manga, ct) is not { } result)
                return null;
            return (Settings.AllowNSFW || result.NSFW != true) ? [result] : null;
        }

        if (searchQuery.Title is not { Length: >= MinimumQueryLength } title)
            return null;

        MyAnimeListSearchResponse? response =
            await Get<MyAnimeListSearchResponse>(
                $"{ApiUrl}/manga?q={Uri.EscapeDataString(title)}&limit=10&fields={Fields}", ct);
        if (response?.Data is not { } entries)
            return null;

        List<Task<SearchResult?>> tasks = entries
            .Where(entry => entry.Node is not null)
            .Select(entry => ParseSearchResult(entry.Node!, ct))
            .ToList();
        await Task.WhenAll(tasks);

        List<SearchResult> ret = [];
        foreach (Task<SearchResult?> task in tasks)
        {
            if (task is { IsCompletedSuccessfully: true, Result: { } result } &&
                (Settings.AllowNSFW || result.NSFW != true))
                ret.Add(result);
        }

        return ret;
    }

    private async Task<T?> Get<T>(string url, CancellationToken ct)
    {
        try
        {
            HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Add("X-MAL-CLIENT-ID", EnvVars.MAL_CLIENT_ID);
            return await MyAnimeListRequestClient.SendAsyncAndParseJson<T>(request, ct);
        }
        catch (Exception)
        {
            return default;
        }
    }

    private async Task<SearchResult?> ParseSearchResult(MyAnimeListManga manga, CancellationToken ct)
    {
        if (manga.Id is not { } id)
            return null;
        string? title = string.IsNullOrWhiteSpace(manga.AlternativeTitles?.En) ? manga.Title : manga.AlternativeTitles.En;
        if (string.IsNullOrWhiteSpace(title))
            return null;
        if ((manga.MainPicture?.Large ?? manga.MainPicture?.Medium) is not { } coverUrl)
            return null;
        if (await GetCover(coverUrl, ct) is not { Length: > 0 } cover)
            return null;

        return new SearchResult()
        {
            MetadataExtensionIdentifier = this.Identifier,
            Identifier = id.ToString(),
            Series = title,
            Summary = manga.Synopsis,
            // start_date is "YYYY", "YYYY-MM" or "YYYY-MM-DD"
            Year = manga.StartDate is { Length: >= 4 } startDate && int.TryParse(startDate[..4], out int year)
                ? year
                : null,
            Authors = AuthorsWithRole(manga, "story"),
            Artists = AuthorsWithRole(manga, "art"),
            Genres = manga.Genres?.Select(genre => genre.Name).Where(name => name is not null).Select(name => name!).ToArray() ?? [],
            Url = $"{BaseUrl}/manga/{id}",
            Cover = cover,
            Status = manga.Status.ParseStatus(),
            // "white" is all-ages; "gray" (borderline) and "black" (explicit) are not
            NSFW = manga.Nsfw is null ? null : manga.Nsfw != "white"
        };
    }

    /// <summary>
    /// Author roles are free text ("Story", "Art", "Story &amp; Art"), so match on the role containing the keyword.
    /// </summary>
    private static string[] AuthorsWithRole(MyAnimeListManga manga, string role) =>
        manga.Authors?
            .Where(author => author.Role?.Contains(role, StringComparison.OrdinalIgnoreCase) is true)
            .Select(author => $"{author.Node?.FirstName} {author.Node?.LastName}".Trim())
            .Where(name => string.IsNullOrWhiteSpace(name) is false)
            .ToArray() ?? [];

    private async Task<TrangaImage?> GetCover(string url, CancellationToken ct)
    {
        try
        {
            Stream data = await MyAnimeListRequestClient.GetStreamAsync(url, ct);
            TrangaImage image = new();
            await data.CopyToAsync(image, ct);
            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }

    #region API response

    private sealed record MyAnimeListSearchResponse(MyAnimeListSearchEntry[]? Data);

    private sealed record MyAnimeListSearchEntry(MyAnimeListManga? Node);

    private sealed record MyAnimeListManga(
        int? Id,
        string? Title,
        [property: JsonPropertyName("main_picture")] MyAnimeListPicture? MainPicture,
        [property: JsonPropertyName("alternative_titles")] MyAnimeListAlternativeTitles? AlternativeTitles,
        [property: JsonPropertyName("start_date")] string? StartDate,
        string? Synopsis,
        MyAnimeListGenre[]? Genres,
        MyAnimeListAuthor[]? Authors,
        string? Status,
        string? Nsfw);

    private sealed record MyAnimeListPicture(string? Medium, string? Large);

    private sealed record MyAnimeListAlternativeTitles(string? En, string? Ja);

    private sealed record MyAnimeListGenre(string? Name);

    private sealed record MyAnimeListAuthor(MyAnimeListAuthorNode? Node, string? Role);

    private sealed record MyAnimeListAuthorNode(
        [property: JsonPropertyName("first_name")] string? FirstName,
        [property: JsonPropertyName("last_name")] string? LastName);

    #endregion
}

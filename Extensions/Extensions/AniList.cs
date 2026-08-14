using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Common.Datatypes;
using Common.Helpers;
using Common.Settings;
using Extensions.Data;

namespace Extensions.Extensions;

/// <summary>
/// Metadata from https://anilist.co/ via its public GraphQL API. No API-key or OpenAPI document exists, so the
/// queries and response DTOs below are hand-written instead of generated.
/// </summary>
public sealed class AniList : IMetadataExtension
{
    public Guid Identifier { get; init; } = Guid.Parse("914c3e45-27f4-45ec-b7e2-88d3827713ce");

    public string Name { get; init; } = "AniList";

    public string BaseUrl { get; init; } = "https://anilist.co";

    public string IconUrl { get; init; } = "https://anilist.co/img/icons/android-chrome-512x512.png";

    private const string GraphQlUrl = "https://graphql.anilist.co";

    // AniList's documented budget is 90 requests/minute, currently degraded to 30. Stay at the lower figure so a
    // search does not start collecting 429s.
    private static readonly RequestClient AniListRequestClient = new(new SlidingWindowRateLimiter(
        new SlidingWindowRateLimiterOptions()
        {
            AutoReplenishment = true,
            Window = TimeSpan.FromSeconds(60),
            SegmentsPerWindow = 6,
            PermitLimit = 30,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));

    private const string MediaFields =
        """
        id
        title { romaji english native }
        description(asHtml: false)
        startDate { year }
        status
        genres
        isAdult
        siteUrl
        coverImage { extraLarge large }
        staff(perPage: 10) { edges { role node { name { full } } } }
        """;

    private static readonly string SearchQueryDocument =
        $$"""
          query ($search: String) {
            Page(page: 1, perPage: 10) {
              media(search: $search, type: MANGA, sort: SEARCH_MATCH) {
                {{MediaFields}}
              }
            }
          }
          """;

    private static readonly string IdQueryDocument =
        $$"""
          query ($id: Int) {
            Media(id: $id, type: MANGA) {
              {{MediaFields}}
            }
          }
          """;

    public async Task<List<SearchResult>?> SearchMetadata(SearchQuery searchQuery, CancellationToken ct)
    {
        // If an AniList ID is included, get the media directly instead of running a fuzzy title search
        if (searchQuery.AniListSeriesId is { } id)
        {
            GraphQlResponse? single = await Post(IdQueryDocument, new Dictionary<string, object> { ["id"] = id }, ct);
            if (single?.Data?.Media is not { } media)
                return null;
            if (await ParseSearchResult(media, ct) is not { } result)
                return null;
            return (Settings.AllowNSFW || result.NSFW != true) ? [result] : null;
        }

        if (string.IsNullOrWhiteSpace(searchQuery.Title))
            return null;

        GraphQlResponse? response =
            await Post(SearchQueryDocument, new Dictionary<string, object> { ["search"] = searchQuery.Title }, ct);
        if (response?.Data?.Page?.Media is not { } mediaList)
            return null;

        List<Task<SearchResult?>> tasks = mediaList.Select(media => ParseSearchResult(media, ct)).ToList();
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

    private async Task<GraphQlResponse?> Post(string query, Dictionary<string, object> variables, CancellationToken ct)
    {
        try
        {
            HttpRequestMessage request = new(HttpMethod.Post, GraphQlUrl)
            {
                Content = JsonContent.Create(new { query, variables })
            };
            return await AniListRequestClient.SendAsyncAndParseJson<GraphQlResponse>(request, ct);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<SearchResult?> ParseSearchResult(AniListMedia media, CancellationToken ct)
    {
        if (media.Id is not { } id)
            return null;
        if ((media.Title?.English ?? media.Title?.Romaji ?? media.Title?.Native) is not { } title)
            return null;
        if ((media.CoverImage?.ExtraLarge ?? media.CoverImage?.Large) is not { } coverUrl)
            return null;
        if (await GetCover(coverUrl, ct) is not { Length: > 0 } cover)
            return null;

        return new SearchResult()
        {
            MetadataExtensionIdentifier = this.Identifier,
            Identifier = id.ToString(),
            Series = title,
            Summary = StripHtml(media.Description),
            Year = media.StartDate?.Year,
            Authors = StaffWithRole(media, "story"),
            Artists = StaffWithRole(media, "art"),
            Genres = media.Genres ?? [],
            Url = media.SiteUrl ?? $"{BaseUrl}/manga/{id}",
            Cover = cover,
            Status = media.Status.ParseStatus(),
            NSFW = media.IsAdult
        };
    }

    /// <summary>
    /// AniList's staff roles are free text ("Story", "Art", "Story &amp; Art", "Translator (French)", ...), so match
    /// on the role containing the keyword and drop the localisation staff that would otherwise land in Authors.
    /// </summary>
    private static string[] StaffWithRole(AniListMedia media, string role) =>
        media.Staff?.Edges?
            .Where(edge => edge.Role?.Contains(role, StringComparison.OrdinalIgnoreCase) is true)
            .Select(edge => edge.Node?.Name?.Full)
            .Where(name => string.IsNullOrWhiteSpace(name) is false)
            .Select(name => name!)
            .ToArray() ?? [];

    /// <summary>
    /// Descriptions come back with the site's inline markup (&lt;br&gt;, &lt;i&gt;, ...) even with
    /// <c>asHtml: false</c>; strip it so the frontend does not render tags as literal text.
    /// </summary>
    private static string? StripHtml(string? description)
    {
        if (description is null)
            return null;
        string withoutBreaks = Regex.Replace(description, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
        string withoutTags = Regex.Replace(withoutBreaks, "<.*?>", string.Empty, RegexOptions.Singleline);
        return withoutTags.Trim();
    }

    private async Task<TrangaImage?> GetCover(string url, CancellationToken ct)
    {
        try
        {
            Stream data = await AniListRequestClient.GetStreamAsync(url, ct);
            TrangaImage image = new();
            await data.CopyToAsync(image, ct);
            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }

    #region GraphQL response

    private sealed record GraphQlResponse([property: JsonPropertyName("data")] AniListData? Data);

    private sealed record AniListData(
        [property: JsonPropertyName("Page")] AniListPage? Page,
        [property: JsonPropertyName("Media")] AniListMedia? Media);

    private sealed record AniListPage([property: JsonPropertyName("media")] AniListMedia[]? Media);

    private sealed record AniListMedia(
        int? Id,
        AniListTitle? Title,
        string? Description,
        AniListDate? StartDate,
        string? Status,
        string[]? Genres,
        bool? IsAdult,
        string? SiteUrl,
        AniListCoverImage? CoverImage,
        AniListStaff? Staff);

    private sealed record AniListTitle(string? Romaji, string? English, string? Native);

    private sealed record AniListDate(int? Year);

    private sealed record AniListCoverImage(string? ExtraLarge, string? Large);

    private sealed record AniListStaff(AniListStaffEdge[]? Edges);

    private sealed record AniListStaffEdge(string? Role, AniListStaffNode? Node);

    private sealed record AniListStaffNode(AniListStaffName? Name);

    private sealed record AniListStaffName(string? Full);

    #endregion
}

using System.Threading.RateLimiting;
using Common.Datatypes;
using Common.Helpers;
using Common.Settings;
using Extensions.Data;
using GeneratedExtensionClients.GeneratedClients.MangaUpdates;

namespace Extensions.Extensions;

public sealed class MangaUpdates : IMetadataExtension
{
    private static readonly RequestClient MangaUpdatesRequestClient = new(new SlidingWindowRateLimiter(
        new SlidingWindowRateLimiterOptions()
        {
            AutoReplenishment = true,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1,
            PermitLimit = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));
    // ReSharper disable once InconsistentNaming
    private readonly MangaUpdatesApiClient Client = new (MangaUpdatesRequestClient);

    public Guid Identifier { get; init; } = Guid.Parse("019cf2cb-3aac-7c9c-9580-7091471b6788");

    public string BaseUrl
    {
        get => Client.BaseUrl;
        // ReSharper disable once ValueParameterNotUsed
        init => Client.BaseUrl = "https://api.mangaupdates.com/";
    }

    public string Name { get; init; } = "MangaUpdates";

    public string IconUrl { get; init; } = "https://www.mangaupdates.com/images/manga-updates.svg";

    /// <summary>
    /// Parses <c>https://www.mangaupdates.com/series/{id}[/slug]</c>, where <c>{id}</c> is the API series-id written
    /// in base 36. Legacy <c>series.html?id=</c> links use a different numbering and are not supported.
    /// </summary>
    public SearchQuery? ParseSearchQueryFromUrl(string url) =>
        UriHelper.GetPathSegmentsOnHost(url, "mangaupdates.com") is ["series", { } id, ..] && TryParseBase36(id, out long seriesId)
            ? new SearchQuery(MangaUpdatesSeriesId: seriesId)
            : null;

    private static bool TryParseBase36(string value, out long result)
    {
        result = 0;
        if (value.Length is 0 or > 12) // 36^12 > long.MaxValue
            return false;
        foreach (char c in value.ToLowerInvariant())
        {
            int digit = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'z' => c - 'a' + 10,
                _ => -1
            };
            if (digit < 0)
                return false;
            result = result * 36 + digit;
        }
        return true;
    }

    public async Task<List<SearchResult>?> SearchMetadata(SearchQuery searchQuery, CancellationToken ct)
    {
        // If MangaUpdates ID is included, try getting the series directly first
        if (searchQuery.MangaUpdatesSeriesId is { } id)
        {
            SeriesModelV1 series;
            try
            {
                series = await Client.RetrieveSeriesAsync(id, cancellationToken: ct);
            }
            catch (MangaUpdatesApiException)
            {
                // Unknown id (e.g. a mistyped pasted URL) - MangaUpdates answers 404.
                return null;
            }
            if (series.Image?.Url?.Original is not { } coverUrl || await GetCover(coverUrl, ct) is not { Length: > 0 } cover)
                return null;
            if(series.Series_id is null)
                return null;
            if(series.Title is null)
                return null;
            ReleaseStatus? status = series.Status.ParseStatus();
            SearchResult sr = new ()
            {
                MetadataExtensionIdentifier = this.Identifier,
                Identifier = series.Series_id.ToString()!,
                Series = series.Title,
                Summary = series.Description,
                Year = series.Year is null ? -1 : int.Parse(series.Year),
                Authors = series.Authors?.Select(a => a.Name).ToArray() ?? [],
                Genres = series.Genres?.Select(g => g.Genre!).ToArray() ?? [],
                Url = series.Url,
                Cover = cover,
                Status = status,
                NSFW = series.Genres?.Any(g => g.Genre?.ToLowerInvariant() == "adult")
            };
            return (Settings.AllowNSFW || sr.NSFW != true) ? [sr] : null;
        }
        
        // Search
        SeriesSearchResponseV1 list = await Client.SearchSeriesPostAsync(new SeriesSearchRequestV1()
        {
            Search = searchQuery.Title,
            Stype = SeriesSearchRequestV1Stype.Title,
            Page = 1,
            Perpage = 10,
            Orderby = SeriesSearchRequestV1Orderby.Score
        }, ct);
        
        if (list.Results is null)
            return null;

        List<(SeriesModelSearchV1 listResult, Task<TrangaImage?> getCoverTask)> tasks = list.Results.Select(r => r.Record)
            .Where(r => r is
            {
                Image: { Url: { Original: not null } },
                Series_id: not null,
                Title: not null
            })
            .Select(r => new ValueTuple<SeriesModelSearchV1, Task<TrangaImage?>>(r!, GetCover(r!.Image!.Url!.Original!, ct)))
            .ToList();
        await Task.WhenAll(tasks.Select(t => t.getCoverTask));

        List<SearchResult> ret = [];
        foreach ((SeriesModelSearchV1 listResult, Task<TrangaImage?> getCoverTask) in tasks.Where(t => t.getCoverTask is { IsCompletedSuccessfully: true, Result: { } }))
        {
            SearchResult sr = new()
            {
                MetadataExtensionIdentifier = this.Identifier,
                Identifier = listResult.Series_id!.ToString()!,
                Series = listResult.Title!,
                Summary = listResult.Description,
                Year = listResult.Year is { } yearStr && int.TryParse(yearStr, out int year) ? year : null,
                Genres = listResult.Genres?.Select(g => g.Genre!).ToArray() ?? [],
                Url = listResult.Url,
                Cover = getCoverTask.Result!,
                NSFW = listResult.Genres?.Any(g => g.Genre?.ToLowerInvariant() == "adult")
            };
            if(Settings.AllowNSFW || sr.NSFW != true)
                ret.Add(sr);
        }

        return ret;
    }

    private async Task<TrangaImage?> GetCover(string url, CancellationToken ct)
    {
        try
        {
            Stream data = await MangaUpdatesRequestClient.GetStreamAsync(url, ct);
            TrangaImage image = new ();
            await data.CopyToAsync(image, ct);
            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
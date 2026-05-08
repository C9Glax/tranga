using System.Threading.RateLimiting;
using Common.Datatypes;
using Common.Helpers;
using Common.Settings;
using Extensions.Data;
using NSwagClients.GeneratedClients.MangaUpdates;

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

    public async Task<List<SearchResult>?> SearchMetadata(SearchQuery searchQuery, CancellationToken ct)
    {
        // If MangaUpdates ID is included, try getting the series directly first
        if (searchQuery.MangaUpdatesSeriesId is { } id)
        {
            SeriesModelV1 series = await Client.RetrieveSeriesAsync(id, cancellationToken: ct);
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

        List<(SeriesModelSearchV1 listResult, Task<MemoryStream?> getCoverTask)> tasks = list.Results.Select(r => r.Record)
            .Where(r => r is
            {
                Image: { Url: { Original: not null } },
                Series_id: not null,
                Title: not null
            })
            .Select(r => new ValueTuple<SeriesModelSearchV1, Task<MemoryStream?>>(r!, GetCover(r!.Image!.Url!.Original!, ct)))
            .ToList();
        await Task.WhenAll(tasks.Select(t => t.getCoverTask));

        List<SearchResult> ret = [];
        foreach ((SeriesModelSearchV1 listResult, Task<MemoryStream?> getCoverTask) in tasks.Where(t => t.getCoverTask is { IsCompletedSuccessfully: true, Result: { } }))
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

    private async Task<MemoryStream?> GetCover(string url, CancellationToken ct)
    {
        try
        {
            Stream data = await MangaUpdatesRequestClient.GetStreamAsync(url, ct);
            MemoryStream ms = new ();
            await data.CopyToAsync(ms, ct);
            return ms;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
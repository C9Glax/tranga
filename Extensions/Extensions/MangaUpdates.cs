using Common.Datatypes;
using Common.Helpers;
using Extensions.Data;
using NSwagClients.GeneratedClients.MangaUpdates;

namespace Extensions.Extensions;

public sealed class MangaUpdates : IMetadataExtension
{
    // ReSharper disable once InconsistentNaming
    private readonly MangaUpdatesApiClient Client = new (new RequestClient());

    public Guid Identifier { get; init; } = Guid.Parse("019cf2cb-3aac-7c9c-9580-7091471b6788");

    public string BaseUrl
    {
        get => Client.BaseUrl;
        init => Client.BaseUrl = "https://api.mangaupdates.com/";
    }

    public string Name { get; init; } = "MangaUpdates";

    public async Task<List<SearchResult>?> SearchMetadata(Common.Datatypes.SearchQuery searchQuery, CancellationToken ct)
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
            return (Settings.Settings.AllowNSFW || sr.NSFW != true) ? [sr] : null;
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

        List<SearchResult> ret = new();
        foreach (SeriesModelSearchV1? listResult in list.Results.Select(r => r.Record))
        {
            if(listResult is null)
                continue;
            if (listResult.Image?.Url?.Original is not { } coverUrl || await GetCover(coverUrl, ct) is not { Length: > 0 } cover)
                continue;
            if(listResult.Series_id is not { } seriesID)
                continue;
            if(listResult.Title is null)
                continue;
            SearchResult sr = new()
            {
                MetadataExtensionIdentifier = this.Identifier,
                Identifier = seriesID.ToString(),
                Series = listResult.Title,
                Summary = listResult.Description,
                Year = listResult.Year is { } yearStr && int.TryParse(yearStr, out int year) ? year : null,
                Genres = listResult.Genres?.Select(g => g.Genre!).ToArray() ?? [],
                Url = listResult.Url,
                Cover = cover,
                NSFW = listResult.Genres?.Any(g => g.Genre?.ToLowerInvariant() == "adult")
            };
            if(Settings.Settings.AllowNSFW || sr.NSFW != true)
                ret.Add(sr);
        }

        return ret;
    }

    private async Task<MemoryStream?> GetCover(string url, CancellationToken ct)
    {
        try
        {
            Stream data = await new RequestClient().GetStreamAsync(url, ct);
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
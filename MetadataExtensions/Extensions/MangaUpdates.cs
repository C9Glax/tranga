using Common.Helpers;
using NSwagClients.GeneratedClients.MangaUpdates;

namespace MetadataExtensions.Extensions;

public sealed record MangaUpdateComicInfo : ComicInfo
{
    public long? MangaUpdatesSeriesId { get; init; }
}

public class MangaUpdates : IMetadataExtension
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

    public async Task<List<ComicInfo>?> Search(Common.Datatypes.SearchQuery searchQuery, CancellationToken ct)
    {
        // If MangaUpdates ID is included, try getting the series directly first
        if (searchQuery.MangaUpdatesSeriesId is { } id)
        {
            SeriesModelV1 series = await Client.RetrieveSeriesAsync(id, cancellationToken: ct);
            if (series.Image?.Url?.Original is not { } coverUrl || await GetCover(coverUrl, ct) is not { Length: > 0 } cover)
                return null;
            return
            [
                new MangaUpdateComicInfo()
                {
                    MangaUpdatesSeriesId = series.Series_id,
                    Series = series.Title,
                    Summary = series.Description,
                    Year = series.Year is null ? -1 : int.Parse(series.Year),
                    Writer = series.Authors is null ? "" : string.Join(',', series.Authors.Select(a => a.Name)),
                    Publisher = series.Publishers is null ? "" : string.Join(',', series.Publishers.Select(p => p.Publisher_name)),
                    Genre = series.Genres is null ? "" : string.Join(',', series.Genres.Select(g => g.Genre)),
                    Web = series.Url,
                    Manga = Common.Datatypes.Manga.Yes,
                    Notes = series.Type.ToString(),
                    Cover = cover
                }
            ];
        }
        
        // Search
        SeriesSearchResponseV1 list = await Client.SearchSeriesPostAsync(new SeriesSearchRequestV1()
        {
            Search = searchQuery.Title,
            Stype = SeriesSearchRequestV1Stype.Title,
            Page = 1,
            Perpage = 10
        }, ct);
        
        if (list.Results is null)
            return null;

        List<ComicInfo> ret = new();
        foreach (SeriesModelSearchV1? listResult in list.Results.Select(r => r.Record))
        {
            if(listResult is null)
                continue;
            if (listResult.Image?.Url?.Original is not { } coverUrl || await GetCover(coverUrl, ct) is not { Length: > 0 } cover)
                return null;
            ret.Add(new MangaUpdateComicInfo()
            {
                MangaUpdatesSeriesId = listResult.Series_id,
                Series = listResult.Title,
                Summary = listResult.Description,
                Year = listResult.Year is null ? -1 : int.Parse(listResult.Year),
                Genre = listResult.Genres is null ? "" : string.Join(',', listResult.Genres.Select(g => g.Genre)),
                Web = listResult.Url,
                Manga = Common.Datatypes.Manga.Yes,
                Notes = listResult.Type.ToString(),
                Cover = cover
            });
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
        catch (Exception e)
        {
            return null;
        }
    }
}
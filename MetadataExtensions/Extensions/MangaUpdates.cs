using Common.Data;
using Common.Helpers;
using Data;
using MetadataExtensions.GeneratedClasses.MangaUpdates;

namespace MetadataExtensions.Extensions;

public class MangaUpdates : IMetadataExtension
{
    // ReSharper disable once InconsistentNaming
    private readonly MangaUpdatesClient Client = new MangaUpdatesClient(new RequestClient());
    
    public string BaseUrl
    {
        get => Client.BaseUrl;
        init => Client.BaseUrl = value;
    }

    public async Task<List<ComicInfo>?> Search(SearchQuery searchQuery, CancellationToken ct)
    {
        // If MangaUpdates ID is included, try getting the series directly first
        if (searchQuery.MangaUpdatesSeriesId is { } id)
        {
            ComicInfo? comicInfo = await GetFromId(id, ct);
            if (comicInfo is not null)
                return [comicInfo];
        }
        
        // Search
        SeriesSearchRequestV1 request = CreateRequest(searchQuery);
        try
        {
            SeriesSearchResponseV1 response = await Client.SearchSeriesPostAsync(request, ct);
            return ParseResponse(response);
        }
        catch (MangaUpdatesApiException)
        {
            return null;
        }
    }
    
    private async Task<ComicInfo?> GetFromId(int id, CancellationToken ct)
    {
        try
        {
            SeriesModelV1 series = await Client.RetrieveSeriesAsync(false, id, ct);
            return ParseSeriesModel(series);
        }
        catch (MangaUpdatesApiException)
        {
            return null;
        }
    }

    private SeriesSearchRequestV1 CreateRequest(SearchQuery searchQuery)
    {
        SeriesSearchRequestV1 request = new()
        {
            Page = 10
        };
        
        if (searchQuery.Title is not null)
        {
            request.Search = searchQuery.Title;
            request.Stype = SeriesSearchRequestV1Stype.Title;
        }
        if (searchQuery.Year is not null)
            request.Year = searchQuery.Year.ToString();
        if (searchQuery.Tags is { Length: > 0 })
            request.Genre = searchQuery.Tags;

        return request;
    }

    private List<ComicInfo> ParseResponse(SeriesSearchResponseV1 response)
    {
        // TODO
        return [];
    }

    private ComicInfo ParseSeriesModel(SeriesModelV1 response) =>
        new ()
        {
            Series = response.Title,
            Year = int.Parse(response.Year),
            // TODO
        };
}
using API.Schema;
using API.Schema.Jobs;
using MangaConnector = Tranga.MangaConnectors.MangaConnector;

namespace JobWorker.Jobs;

public class SearchManga(SearchMangaJob data) : Job<SearchMangaJob>(data)
{
    
    private const string CreateMangaEndpoint = "v2/Manga";
    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal(SearchMangaJob data)
    {
        MangaConnector mangaConnector = GetConnector(data.MangaConnectorName);
        foreach ((Manga, Author[], MangaTag[], Link[], MangaAltTitle[]) valueTuple in mangaConnector.GetManga(
                     data.SearchString))
            Monitor.MakePutRequestApi(CreateMangaEndpoint, valueTuple.Item1, out object? _);
        return [];
    }
}
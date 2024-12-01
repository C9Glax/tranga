namespace API.Schema.Jobs;

public class SearchMangaJob(
    string searchString,
    string? mangaConnectorId = null,
    string? parentJobId = null,
    string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(SearchMangaJob), 64), JobType.SearchManga, 0, parentJobId, dependsOnJobIds)
{
    public string SearchString { get; init; } = searchString;
    
    public string MangaConnectorId { get; init; } = mangaConnectorId;
    public virtual MangaConnector MangaConnector { get; }
}
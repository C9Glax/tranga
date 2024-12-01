namespace API.Schema.Jobs;

public class SearchMangaJob(
    string searchString,
    string? mangaConnectorName = null,
    string? parentJobId = null,
    string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(SearchMangaJob), 64), JobType.SearchMangaJob, 0, parentJobId, dependsOnJobIds)
{
    public string SearchString { get; init; } = searchString;
    
    public string MangaConnectorName { get; init; } = mangaConnectorName;
    public virtual MangaConnector MangaConnector { get; }
}
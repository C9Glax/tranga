using API.Schema.Jobs;

namespace JobWorker.Jobs;

public class SearchManga(SearchMangaJob data) : Job<SearchMangaJob>(data)
{
    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal(SearchMangaJob data)
    {
        throw new NotImplementedException();
    }
}
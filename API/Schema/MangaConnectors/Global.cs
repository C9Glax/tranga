using API.Schema.Contexts;

namespace API.Schema.MangaConnectors;

public class Global : MangaConnector
{
    private PgsqlContext context { get; init; }
    public Global(PgsqlContext context) : base("Global", ["all"], [""], "")
    {
        this.context = context;
    }

    public override MangaConnectorMangaEntry[] SearchManga(string mangaSearchName)
    {
        //Get all enabled Connectors
        MangaConnector[] enabledConnectors = context.MangaConnectors.Where(c => c.Enabled && c.Name != "Global").ToArray();
        
        //Create Task for each MangaConnector to search simulatneously
        Task<MangaConnectorMangaEntry[]>[] tasks =
            enabledConnectors.Select(c => new Task<MangaConnectorMangaEntry[]>(() => c.SearchManga(mangaSearchName))).ToArray();
        foreach (var task in tasks)
            task.Start();
        
        //Wait for all tasks to finish
        do
        {
            Thread.Sleep(50);
        }while(tasks.Any(t => t.Status < TaskStatus.RanToCompletion));
        
        //Concatenate all results into one
        MangaConnectorMangaEntry[] ret = tasks.Select(t => t.IsCompletedSuccessfully ? t.Result : []).ToArray().SelectMany(i => i).ToArray();
        return ret;
    }

    public override MangaConnectorMangaEntry? GetMangaFromUrl(string url)
    {
        MangaConnector? mc = context.MangaConnectors.ToArray().FirstOrDefault(c => c.UrlMatchesConnector(url));
        return mc?.GetMangaFromUrl(url) ?? null;
    }

    public override MangaConnectorMangaEntry? GetMangaFromId(string mangaIdOnSite)
    {
        return null;
    }

    public override Chapter[] GetChapters(MangaConnectorMangaEntry mangaConnectorMangaEntry, string? language = null)
    {
        return mangaConnectorMangaEntry.MangaConnector.GetChapters(mangaConnectorMangaEntry, language);
    }

    internal override string[] GetChapterImageUrls(Chapter chapter)
    {
        return chapter.MangaConnectorMangaEntry.MangaConnector.GetChapterImageUrls(chapter);
    }
}
using API.Schema.Contexts;

namespace API.Schema.MangaConnectors;

public class Global : MangaConnector
{
    private PgsqlContext context { get; init; }
    public Global(PgsqlContext context) : base("Global", ["all"], [""], "")
    {
        this.context = context;
    }

    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        //Get all enabled Connectors
        MangaConnector[] enabledConnectors = context.MangaConnectors.Where(c => c.Enabled && c.Name != "Global").ToArray();
        
        //Create Task for each MangaConnector to search simultaneously
        Task<(Manga, MangaConnectorId<Manga>)[]>[] tasks =
            enabledConnectors.Select(c => new Task<(Manga, MangaConnectorId<Manga>)[]>(() => c.SearchManga(mangaSearchName))).ToArray();
        foreach (var task in tasks)
            task.Start();
        
        //Wait for all tasks to finish
        do
        {
            Thread.Sleep(50);
        }while(tasks.Any(t => t.Status < TaskStatus.RanToCompletion));
        
        //Concatenate all results into one
        (Manga, MangaConnectorId<Manga>)[] ret = tasks.Select(t => t.IsCompletedSuccessfully ? t.Result : []).ToArray().SelectMany(i => i).ToArray();
        return ret;
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
        MangaConnector? mc = context.MangaConnectors.ToArray().FirstOrDefault(c => c.UrlMatchesConnector(url));
        return mc?.GetMangaFromUrl(url) ?? null;
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
        return null;
    }

    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> manga,
        string? language = null)
    {
        return manga.MangaConnector.GetChapters(manga, language);
    }

    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        return chapterId.MangaConnector.GetChapterImageUrls(chapterId);
    }
}
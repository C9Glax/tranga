using API.Schema.MangaContext;

namespace API.MangaConnectors;

public class Global : MangaConnector
{
    public Global() : base("Global", ["all"], [""], "")
    {
    }

    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        //Get all enabled Connectors
        MangaConnector[] enabledConnectors = Tranga.MangaConnectors.Where(c => c.Enabled && c.Name != "Global").ToArray();
        
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
        MangaConnector? mc = Tranga.MangaConnectors.FirstOrDefault(c => c.UrlMatchesConnector(url));
        return mc?.GetMangaFromUrl(url) ?? null;
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
        return null;
    }

    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> manga,
        string? language = null)
    {
        if (!Tranga.TryGetMangaConnector(manga.MangaConnectorName, out MangaConnector? mangaConnector))
            return [];
        return mangaConnector.GetChapters(manga, language);
    }

    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        if (!Tranga.TryGetMangaConnector(chapterId.MangaConnectorName, out MangaConnector? mangaConnector))
            return [];
        return mangaConnector.GetChapterImageUrls(chapterId);
    }
}
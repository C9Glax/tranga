namespace API.Schema.MangaConnectors;

public class Global : MangaConnector
{
    private PgsqlContext context { get; init; }
    public Global(PgsqlContext context) : base("Global", ["all"], [""], "")
    {
        this.context = context;
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(string publicationTitle = "")
    {
        //Get all enabled Connectors
        MangaConnector[] enabledConnectors = context.MangaConnectors.Where(c => c.Enabled && c.Name != "Global").ToArray();
        
        //Create Task for each MangaConnector to search simulatneously
        Task<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[]>[] tasks =
            enabledConnectors.Select(c =>
                new Task<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[]>(() => c.GetManga(publicationTitle))).ToArray();
        foreach (var task in tasks)
            task.Start();
        
        //Wait for all tasks to finish
        do
        {
            Thread.Sleep(50);
        }while(tasks.Any(t => t.Status < TaskStatus.RanToCompletion));
        
        //Concatenate all results into one
        (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] ret = 
            tasks.Select(t => t.IsCompletedSuccessfully ? t.Result : []).ToArray().SelectMany(i => i).ToArray();
        return ret;
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromUrl(string url)
    {
        MangaConnector? mc = context.MangaConnectors.ToArray().FirstOrDefault(c => c.ValidateUrl(url));
        return mc?.GetMangaFromUrl(url) ?? null;
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromId(string publicationId)
    {
        return null;
    }

    public override Chapter[] GetChapters(Manga manga, string language = "en")
    {
        return manga.MangaConnector?.GetChapters(manga) ?? [];
    }

    internal override string[] GetChapterImageUrls(Chapter chapter)
    {
        return chapter.ParentManga?.MangaConnector?.GetChapterImageUrls(chapter) ?? [];
    }
}
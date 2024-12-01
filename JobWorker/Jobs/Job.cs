using API.Schema;
using API.Schema.Jobs;
using log4net;
using log4net.Config;
using Tranga.MangaConnectors;
using MangaConnector = Tranga.MangaConnectors.MangaConnector;

namespace JobWorker.Jobs;

public abstract class Job<T> where T : Job
{
    protected readonly ILog Log;
    private readonly T _data;

    public Job(T data)
    {
        this._data = data;
        
        Log = LogManager.GetLogger(this.GetType());
        BasicConfigurator.Configure();
    }

    public void Execute(out Job[] newJobs)
    {
        Job[] jobs = ExecuteReturnSubTasksInternal(this._data).ToArray();
        newJobs = jobs;
    }

    protected abstract IEnumerable<Job> ExecuteReturnSubTasksInternal(T data);

    protected MangaConnector GetConnector(Manga manga)
    {
        return manga.MangaConnector.Name switch
        {
            "AsuraToon" => new AsuraToon(manga.MangaConnectorName),
            "Bato" => new Bato(manga.MangaConnectorName),
            "MangaDex" => new MangaDex(manga.MangaConnectorName),
            "MangaHere" => new MangaHere(manga.MangaConnectorName),
            "MangaKatana" => new MangaKatana(manga.MangaConnectorName),
            "MangaLife" => new MangaLife(manga.MangaConnectorName),
            "Manganato" => new Manganato(manga.MangaConnectorName),
            "Mangasee" => new Mangasee(manga.MangaConnectorName),
            "Mangaworld" => new Mangaworld(manga.MangaConnectorName),
            "ManhuaPlus" => new ManhuaPlus(manga.MangaConnectorName),
            _ => throw new KeyNotFoundException($"Could not find connector with name {manga.MangaConnector.Name}")
        };
    }

    protected MangaConnector GetConnector(Chapter chapter) => GetConnector(chapter.ParentManga);
}
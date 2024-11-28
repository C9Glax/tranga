using API.Schema;
using API.Schema.Jobs;
using log4net;
using log4net.Config;
using Tranga.MangaConnectors;
using MangaConnector = Tranga.MangaConnectors.MangaConnector;

namespace JobWorker.Jobs;

public abstract class Job<I, O>
{
    protected readonly ILog Log;
    public O returnValue { get; protected set; }

    public Job()
    {
        Log = LogManager.GetLogger(this.GetType());
        BasicConfigurator.Configure();
    }

    public IEnumerable<Job> Execute(I data, Job[] relatedJobs)
    {
        (IEnumerable<Job> jobs, O ret) = ExecuteReturnSubTasksInternal(data, relatedJobs);
        returnValue = ret;
        return jobs;
    }

    protected abstract ValueTuple<IEnumerable<Job>, O> ExecuteReturnSubTasksInternal(I data, Job[] relatedJobs);

    protected MangaConnector GetConnector(Manga manga)
    {
        return manga.MangaConnector.Name switch
        {
            "AsuraToon" => new AsuraToon(manga.MangaConnectorId),
            "Bato" => new Bato(manga.MangaConnectorId),
            "MangaDex" => new MangaDex(manga.MangaConnectorId),
            "MangaHere" => new MangaHere(manga.MangaConnectorId),
            "MangaKatana" => new MangaKatana(manga.MangaConnectorId),
            "MangaLife" => new MangaLife(manga.MangaConnectorId),
            "Manganato" => new Manganato(manga.MangaConnectorId),
            "Mangasee" => new Mangasee(manga.MangaConnectorId),
            "Mangaworld" => new Mangaworld(manga.MangaConnectorId),
            "ManhuaPlus" => new ManhuaPlus(manga.MangaConnectorId),
            _ => throw new KeyNotFoundException($"Could not find connector with name {manga.MangaConnector.Name}")
        };
    }

    protected MangaConnector GetConnector(Chapter chapter) => GetConnector(chapter.ParentManga);
}
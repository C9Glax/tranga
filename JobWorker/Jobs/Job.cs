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

    private const string UpdateJobStatusEndpoint = "v2/Job/{0}/Status";
    public void Execute(out Job[] newJobs)
    {
        Monitor.MakePatchRequestApi(string.Format(UpdateJobStatusEndpoint, _data.JobId), JobState.Running, out object? _);
        Job[] jobs = ExecuteReturnSubTasksInternal(this._data).ToArray();
        newJobs = jobs;
    }

    protected abstract IEnumerable<Job> ExecuteReturnSubTasksInternal(T data);

    protected MangaConnector GetConnector(Manga manga) => GetConnector(manga.MangaConnectorName);

    protected MangaConnector GetConnector(Chapter chapter) => GetConnector(chapter.ParentManga);
    
    protected MangaConnector GetConnector(API.Schema.MangaConnector connector) => GetConnector(connector.Name);
    
    protected MangaConnector GetConnector(string connectorName) => connectorName switch
    {
        "AsuraToon" => new AsuraToon(connectorName),
        "Bato" => new Bato(connectorName),
        "MangaDex" => new MangaDex(connectorName),
        "MangaHere" => new MangaHere(connectorName),
        "MangaKatana" => new MangaKatana(connectorName),
        "MangaLife" => new MangaLife(connectorName),
        "Manganato" => new Manganato(connectorName),
        "Mangasee" => new Mangasee(connectorName),
        "Mangaworld" => new Mangaworld(connectorName),
        "ManhuaPlus" => new ManhuaPlus(connectorName),
        _ => throw new KeyNotFoundException($"Could not find connector with name {connectorName}")
    };
}
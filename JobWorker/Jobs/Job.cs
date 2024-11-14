using API.Schema;
using API.Schema.Jobs;
using log4net;
using log4net.Config;
using MangaConnector = Tranga.MangaConnectors.MangaConnector;

namespace JobWorker.Jobs;

public abstract class Job<I, O>
{
    protected readonly ILog log;

    public Job()
    {
        log = LogManager.GetLogger(this.GetType());
        BasicConfigurator.Configure();
    }

    protected abstract ValueTuple<IEnumerable<Job>, O> ExecuteReturnSubTasksInternal(I data);

    protected MangaConnector GetConnector(Manga manga)
    {
        
    }

    protected MangaConnector GetConnector(Chapter chapter) => GetConnector(chapter.ParentManga);
}
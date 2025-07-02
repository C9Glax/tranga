using API.Schema.MangaContext.MetadataFetchers;
using API.Workers;
using log4net;
using log4net.Config;

namespace API;

public static class Tranga
{

    // ReSharper disable once InconsistentNaming
    private const string TRANGA = 
        "\n\n" +
        " _______                                 v2\n" +
        "|_     _|.----..---.-..-----..-----..---.-.\n" +
        "  |   |  |   _||  _  ||     ||  _  ||  _  |\n" +
        "  |___|  |__|  |___._||__|__||___  ||___._|\n" +
        "                             |_____|       \n\n";
    
    public static Thread PeriodicWorkerStarterThread { get; } = new (WorkerStarter);
    private static readonly ILog Log = LogManager.GetLogger(typeof(Tranga));
    internal static readonly MetadataFetcher[] MetadataFetchers = [new MyAnimeList()];

    internal static void StartLogger()
    {
        BasicConfigurator.Configure();
        Log.Info("Logger Configured.");
        Log.Info(TRANGA);
    }
    
    internal static HashSet<BaseWorker> Workers { get; private set; } = new ();
    public static void AddWorker(BaseWorker worker) => Workers.Add(worker);
    public static void AddWorkers(IEnumerable<BaseWorker> workers)
    {
        foreach (BaseWorker baseWorker in workers)
        {
            AddWorker(baseWorker);
        }
    }
    public static void RemoveWorker(BaseWorker worker)
    {
        IEnumerable<BaseWorker> baseWorkers = Workers.Where(w => w.DependenciesAndSelf.Any(w => w == worker));
        foreach (BaseWorker baseWorker in baseWorkers)
        {
             baseWorker.Cancel();
             Workers.Remove(baseWorker);
             if (RunningWorkers.ContainsKey(baseWorker))
             {
                 RunningWorkers[baseWorker].Abort();
                 RunningWorkers.Remove(baseWorker);
             }
        }
    }

    private static readonly Dictionary<BaseWorker, Thread> RunningWorkers = new();
    public static BaseWorker[] GetRunningWorkers() => RunningWorkers.Keys.ToArray();
    private static void WorkerStarter(object? serviceProviderObj)
    {
        Log.Info("WorkerStarter Thread running.");
        if (serviceProviderObj is null)
        {
            Log.Error("serviceProviderObj is null");
            return;
        }
        IServiceProvider serviceProvider = (IServiceProvider)serviceProviderObj;
        
        while (true)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
           
            Thread.Sleep(TrangaSettings.workCycleTimeout);
        }
    }
}
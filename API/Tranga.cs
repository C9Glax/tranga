using API.Schema.MangaContext.MetadataFetchers;
using API.Workers;
using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;

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
    
    internal static void StopWorker(BaseWorker worker) => RemoveWorker(worker);
    
    public static void RemoveWorker(BaseWorker removeWorker)
    {
        IEnumerable<BaseWorker> baseWorkers = Workers.Where(w => w.DependenciesAndSelf.Any(worker => worker == removeWorker));
        
        foreach (BaseWorker worker in baseWorkers)
        {
             worker.Cancel();
             Workers.Remove(worker);
             if (RunningWorkers.ContainsKey(worker))
             {
                 worker.Cancel();
                 RunningWorkers.Remove(worker);
             }
        }
    }

    private static readonly Dictionary<BaseWorker, Task<BaseWorker[]>> RunningWorkers = new();
    public static BaseWorker[] GetRunningWorkers() => RunningWorkers.Keys.ToArray();
    private static readonly HashSet<BaseWorker> StartWorkers = new();
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
            CheckRunningWorkers();
            
            foreach (BaseWorker worker in StartWorkers)
            {
                if (worker is BaseWorkerWithContext<DbContext> scopedWorker)
                    scopedWorker.SetScope(serviceProvider.CreateScope());
                RunningWorkers.Add(worker, worker.DoWork());
            }
            Thread.Sleep(TrangaSettings.workCycleTimeout);
        }
    }

    private static void CheckRunningWorkers()
    {
        KeyValuePair<BaseWorker, Task<BaseWorker[]>>[] done = RunningWorkers.Where(kv => kv.Value.IsCompleted).ToArray();
        Log.Info($"Done: {done.Length}");
        Log.Debug(string.Join("\n", done.Select(d => d.ToString())));
        foreach ((BaseWorker worker, Task<BaseWorker[]> task) in done)
        {
            RunningWorkers.Remove(worker);
            foreach (BaseWorker newWorker in task.Result)
                StartWorkers.Add(newWorker);
            task.Dispose();
        }
    }

    internal static void MarkWorkerForStart(BaseWorker worker) => StartWorkers.Add(worker);
}
using System.Diagnostics.CodeAnalysis;
using API.Schema.MangaContext;
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
    internal static TrangaSettings Settings = TrangaSettings.Load();

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
            Thread.Sleep(Settings.WorkCycleTimeoutMs);
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
    
    internal static bool AddMangaToContext((Manga, MangaConnectorId<Manga>) addManga, MangaContext context, [NotNullWhen(true)]out Manga? manga) => AddMangaToContext(addManga.Item1, addManga.Item2, context, out manga);

    internal static bool AddMangaToContext(Manga addManga, MangaConnectorId<Manga> addMcId, MangaContext context, [NotNullWhen(true)]out Manga? manga)
    {
        manga = context.Mangas.Find(addManga.Key) ?? addManga;
        MangaConnectorId<Manga> mcId = context.MangaConnectorToManga.Find(addMcId.Key) ?? addMcId;
        mcId.Obj = manga;
        
        IEnumerable<MangaTag> mergedTags = manga.MangaTags.Select(mt =>
        {
            MangaTag? inDb = context.Tags.Find(mt.Tag);
            return inDb ?? mt;
        });
        manga.MangaTags = mergedTags.ToList();

        IEnumerable<Author> mergedAuthors = manga.Authors.Select(ma =>
        {
            Author? inDb = context.Authors.Find(ma.Key);
            return inDb ?? ma;
        });
        manga.Authors = mergedAuthors.ToList();
        
        if(context.MangaConnectorToManga.Find(addMcId.Key) is null)
            context.MangaConnectorToManga.Add(mcId);

        if (context.Sync().Result is { success: false })
            return false;
        return true;
    }

    internal static bool AddChapterToContext((Chapter, MangaConnectorId<Chapter>) addChapter, MangaContext context,
        [NotNullWhen(true)] out Chapter? chapter) => AddChapterToContext(addChapter.Item1, addChapter.Item2, context, out chapter);

    internal static bool AddChapterToContext(Chapter addChapter, MangaConnectorId<Chapter> addChId, MangaContext context, [NotNullWhen(true)] out Chapter? chapter)
    {
        chapter = context.Chapters.Find(addChapter.Key) ?? addChapter;
        MangaConnectorId<Chapter> chId = context.MangaConnectorToChapter.Find(addChId.Key) ?? addChId;
        chId.Obj = chapter;
        
        if(context.MangaConnectorToChapter.Find(chId.Key) is null)
            context.MangaConnectorToChapter.Add(chId);

        if (context.Sync().Result is { success: false })
            return false;
        return true;
    }
}
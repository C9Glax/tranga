using System.Diagnostics.CodeAnalysis;
using API.MangaConnectors;
using API.Schema.LibraryContext;
using API.Schema.MangaContext;
using API.Schema.MangaContext.MetadataFetchers;
using API.Schema.NotificationsContext;
using API.Workers;
using API.Workers.MaintenanceWorkers;
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
    internal static readonly MangaConnector[] MangaConnectors = [new Global(), new MangaDex(), new ComickIo()];
    internal static TrangaSettings Settings = TrangaSettings.Load();
    
    internal static readonly UpdateMetadataWorker UpdateMetadataWorker = new ();
    internal static readonly SendNotificationsWorker SendNotificationsWorker = new();
    internal static readonly UpdateChaptersDownloadedWorker UpdateChaptersDownloadedWorker = new();
    internal static readonly CheckForNewChaptersWorker CheckForNewChaptersWorker = new();
    internal static readonly CleanupMangaCoversWorker CleanupMangaCoversWorker = new();
    internal static readonly StartNewChapterDownloadsWorker StartNewChapterDownloadsWorker = new();
    internal static readonly RemoveOldNotificationsWorker RemoveOldNotificationsWorker = new();
    internal static readonly UpdateCoversWorker UpdateCoversWorker = new();

    internal static void StartLogger()
    {
        BasicConfigurator.Configure();
        Log.Info("Logger Configured.");
        Log.Info(TRANGA);
    }

    internal static void AddDefaultWorkers()
    {
        AddWorker(UpdateMetadataWorker);
        AddWorker(SendNotificationsWorker);
        AddWorker(UpdateChaptersDownloadedWorker);
        AddWorker(CheckForNewChaptersWorker);
        AddWorker(CleanupMangaCoversWorker);
        AddWorker(StartNewChapterDownloadsWorker);
        AddWorker(RemoveOldNotificationsWorker);
        AddWorker(UpdateCoversWorker);
    }

    internal static bool TryGetMangaConnector(string name, [NotNullWhen(true)]out MangaConnector? mangaConnector)
    {
        mangaConnector =
            MangaConnectors.FirstOrDefault(c => c.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        return mangaConnector != null;
    }
    
    internal static HashSet<BaseWorker> AllWorkers { get; private set; } = new ();

    public static void AddWorker(BaseWorker worker)
    {
        Log.Debug($"Adding {worker} to AllWorkers.");
        AllWorkers.Add(worker);
    }
    public static void AddWorkers(IEnumerable<BaseWorker> workers)
    {
        foreach (BaseWorker baseWorker in workers)
        {
            AddWorker(baseWorker);
        }
    }
    
    public static void RemoveWorker(BaseWorker removeWorker)
    {
        IEnumerable<BaseWorker> baseWorkers = AllWorkers.Where(w => w.DependenciesAndSelf.Any(worker => worker == removeWorker));
        
        foreach (BaseWorker worker in baseWorkers)
        {
             StopWorker(worker);
             Log.Debug($"Removing {removeWorker} from AllWorkers.");
             AllWorkers.Remove(worker);
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

            foreach (BaseWorker baseWorker in AllWorkers.DueWorkers().ToArray())
                StartWorkers.Add(baseWorker);
            
            foreach (BaseWorker worker in StartWorkers.ToArray())
            {
                if(RunningWorkers.ContainsKey(worker))
                    continue;
                if (worker is BaseWorkerWithContext<MangaContext> mangaContextWorker)
                {
                    mangaContextWorker.SetScope(serviceProvider.CreateScope());
                    RunningWorkers.Add(mangaContextWorker, mangaContextWorker.DoWork());
                }else if (worker is BaseWorkerWithContext<NotificationsContext> notificationContextWorker)
                {
                    notificationContextWorker.SetScope(serviceProvider.CreateScope());
                    RunningWorkers.Add(notificationContextWorker, notificationContextWorker.DoWork());
                }else if (worker is BaseWorkerWithContext<LibraryContext> libraryContextWorker)
                {
                    libraryContextWorker.SetScope(serviceProvider.CreateScope());
                    RunningWorkers.Add(libraryContextWorker, libraryContextWorker.DoWork());
                }else
                    RunningWorkers.Add(worker, worker.DoWork());

                StartWorkers.Remove(worker);
            }
            Thread.Sleep(Settings.WorkCycleTimeoutMs);
        }
    }

    private static void CheckRunningWorkers()
    {
        KeyValuePair<BaseWorker, Task<BaseWorker[]>>[] done = RunningWorkers.Where(kv => kv.Value.IsCompleted).ToArray();
        if (done.Length < 1)
            return;
        Log.Info($"Done: {done.Length}");
        Log.Debug(string.Join("\n", done.Select(d => d.Key.ToString())));
        foreach ((BaseWorker worker, Task<BaseWorker[]> task) in done)
        {
            RunningWorkers.Remove(worker);
            foreach (BaseWorker newWorker in task.Result)
                AllWorkers.Add(newWorker);
            if (worker is not IPeriodic)
                AllWorkers.Remove(worker);
            task.Dispose();
        }
    }

    private static IEnumerable<BaseWorker> DueWorkers(this IEnumerable<BaseWorker> workers)
    {
        return workers.Where(worker =>
        {
            if (worker.State is >= WorkerExecutionState.Running and < WorkerExecutionState.Completed)
                return false;
            if (worker is IPeriodic periodicWorker)
                return periodicWorker.IsDue;
            return true;
        });
    }

    internal static void MarkWorkerForStart(BaseWorker worker) => StartWorkers.Add(worker);

    internal static void StopWorker(BaseWorker worker)
    {
        StartWorkers.Remove(worker);
        worker.Cancel();
        RunningWorkers.Remove(worker);
    }
    
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

        if (context.Sync() is { success: false })
            return false;

        DownloadCoverFromMangaconnectorWorker downloadCoverWorker = new (addMcId);
        AddWorker(downloadCoverWorker);
        
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

        if (context.Sync() is { success: false })
            return false;
        return true;
    }
}
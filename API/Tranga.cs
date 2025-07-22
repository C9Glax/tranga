using System.Collections.Concurrent;
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
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
    
    private static IServiceProvider? ServiceProvider;
    
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

    internal static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    internal static bool TryGetMangaConnector(string name, [NotNullWhen(true)]out MangaConnector? mangaConnector)
    {
        mangaConnector =
            MangaConnectors.FirstOrDefault(c => c.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        return mangaConnector != null;
    }
    
    internal static readonly ConcurrentDictionary<IPeriodic, Task> PeriodicWorkers = new ();

    public static void AddWorker(BaseWorker worker)
    {
        Log.Debug($"Adding Worker {worker}");
        StartWorker(worker);
        if(worker is IPeriodic periodic)
            AddPeriodicWorker(worker, periodic);
    }

    private static void AddPeriodicWorker(BaseWorker worker, IPeriodic periodic)
    {
        Log.Debug($"Adding Periodic {worker}");
        Task periodicTask = PeriodicTask(worker, periodic);
        PeriodicWorkers.TryAdd((worker as IPeriodic)!, periodicTask);
        periodicTask.Start();
    }

    private static Task PeriodicTask(BaseWorker worker, IPeriodic periodic) => new (() =>
    {
        Log.Debug($"Waiting {periodic.Interval} for next run of {worker}");
        Thread.Sleep(periodic.Interval);
        StartWorker(worker, RefreshTask(worker, periodic));
    });

    private static Action RefreshTask(BaseWorker worker, IPeriodic periodic) => () =>
    {
        if (worker.State < WorkerExecutionState.Created) //Failed
            return;
        Log.Debug($"Refreshing {worker}");
        Task periodicTask = PeriodicTask(worker, periodic);
        PeriodicWorkers.AddOrUpdate((worker as IPeriodic)!, periodicTask, (_, _) => periodicTask);
        periodicTask.Start();
    };
    
    public static void AddWorkers(IEnumerable<BaseWorker> workers)
    {
        foreach (BaseWorker baseWorker in workers)
            AddWorker(baseWorker);
    }

    private static readonly ConcurrentDictionary<BaseWorker, Task<BaseWorker[]>> RunningWorkers = new();
    public static BaseWorker[] GetRunningWorkers() => RunningWorkers.Keys.ToArray();
    
    internal static void StartWorker(BaseWorker worker, Action? callback = null)
    {
        Log.Debug($"Starting {worker}");
        if (ServiceProvider is null)
        {
            Log.Fatal("ServiceProvider is null");
            return;
        }
        Action afterWorkCallback = AfterWork(worker, callback);
        
        if (worker is BaseWorkerWithContext<MangaContext> mangaContextWorker)
        {
            mangaContextWorker.SetScope(ServiceProvider.CreateScope());
            RunningWorkers.TryAdd(mangaContextWorker, mangaContextWorker.DoWork(afterWorkCallback));
        }else if (worker is BaseWorkerWithContext<NotificationsContext> notificationContextWorker)
        {
            notificationContextWorker.SetScope(ServiceProvider.CreateScope());
            RunningWorkers.TryAdd(notificationContextWorker, notificationContextWorker.DoWork(afterWorkCallback));
        }else if (worker is BaseWorkerWithContext<LibraryContext> libraryContextWorker)
        {
            libraryContextWorker.SetScope(ServiceProvider.CreateScope());
            RunningWorkers.TryAdd(libraryContextWorker, libraryContextWorker.DoWork(afterWorkCallback));
        }else
            RunningWorkers.TryAdd(worker, worker.DoWork(afterWorkCallback));
    }

    private static Action AfterWork(BaseWorker worker, Action? callback = null) => () =>
    {
        Log.Debug($"AfterWork {worker}");
        RunningWorkers.Remove(worker, out _);
        callback?.Invoke();
    };

    internal static void StopWorker(BaseWorker worker)
    {
        Log.Debug($"Stopping {worker}");
        if(worker is IPeriodic periodicWorker)
            PeriodicWorkers.Remove(periodicWorker, out _);
        worker.Cancel();
        RunningWorkers.Remove(worker, out _);
    }
    
    internal static bool AddMangaToContext((Manga, MangaConnectorId<Manga>) addManga, MangaContext context, [NotNullWhen(true)]out Manga? manga) => AddMangaToContext(addManga.Item1, addManga.Item2, context, out manga);

    internal static bool AddMangaToContext(Manga addManga, MangaConnectorId<Manga> addMcId, MangaContext context, [NotNullWhen(true)]out Manga? manga)
    {
        manga = context.Mangas.Find(addManga.Key) ?? addManga;
        MangaConnectorId<Manga> mcId = context.MangaConnectorToManga.Find(addMcId.Key) ?? addMcId;
        mcId.Obj = manga;
        
        foreach (CollectionEntry collectionEntry in context.Entry(manga).Collections)
            collectionEntry.Load();
        context.Entry(manga).Navigation(nameof(Manga.Library)).Load();
        
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
        
        foreach (CollectionEntry collectionEntry in context.Entry(chapter).Collections)
            collectionEntry.Load();
        context.Entry(chapter).Navigation(nameof(Chapter.ParentManga)).Load();
        
        if(context.MangaConnectorToChapter.Find(chId.Key) is null)
            context.MangaConnectorToChapter.Add(chId);

        if (context.Sync() is { success: false })
            return false;
        return true;
    }
}
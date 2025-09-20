using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using API.MangaConnectors;
using API.Schema.LibraryContext;
using API.Schema.MangaContext;
using API.Schema.MangaContext.MetadataFetchers;
using API.Schema.NotificationsContext;
using API.Workers;
using API.Workers.MangaDownloadWorkers;
using API.Workers.PeriodicWorkers;
using API.Workers.PeriodicWorkers.MaintenanceWorkers;
using log4net;
using log4net.Config;

namespace API;

public static class Tranga
{
    internal static IServiceProvider? ServiceProvider { get; set; }
    
    private static readonly ILog Log = LogManager.GetLogger(typeof(Tranga));
    internal static readonly MetadataFetcher[] MetadataFetchers = [new MyAnimeList()];
    internal static readonly MangaConnector[] MangaConnectors = [new Global(), new MangaDex(), new Mangaworld(), new MangaPark()];
    internal static TrangaSettings Settings = TrangaSettings.Load();
    
    // ReSharper disable MemberCanBePrivate.Global
    internal static readonly UpdateMetadataWorker UpdateMetadataWorker = new ();
    internal static readonly SendNotificationsWorker SendNotificationsWorker = new();
    internal static readonly UpdateChaptersDownloadedWorker UpdateChaptersDownloadedWorker = new();
    internal static readonly CheckForNewChaptersWorker CheckForNewChaptersWorker = new();
    internal static readonly CleanupMangaCoversWorker CleanupMangaCoversWorker = new();
    internal static readonly StartNewChapterDownloadsWorker StartNewChapterDownloadsWorker = new();
    internal static readonly RemoveOldNotificationsWorker RemoveOldNotificationsWorker = new();
    internal static readonly UpdateCoversWorker UpdateCoversWorker = new();
    internal static readonly UpdateLibraryConnectorsWorker UpdateLibraryConnectorsWorker = new();
    internal static readonly CleanupMangaconnectorIdsWithoutConnector CleanupMangaconnectorIdsWithoutConnector = new();
    // ReSharper restore MemberCanBePrivate.Global

    internal static void StartLogger(FileInfo loggerConfigFile)
    {
        XmlConfigurator.ConfigureAndWatch(loggerConfigFile);
        Log.Info("Logger Configured.");
        Log.Info(Constants.TRANGA);
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
        AddWorker(UpdateLibraryConnectorsWorker);
        AddWorker(CleanupMangaconnectorIdsWithoutConnector);
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
        Task periodicTask = RefreshedPeriodicTask(worker, periodic);
        PeriodicWorkers.TryAdd((worker as IPeriodic)!, periodicTask);
        periodicTask.Start();
    }

    private static Task RefreshedPeriodicTask(BaseWorker worker, IPeriodic periodic) => new (() =>
    {
        Log.Debug($"Waiting {periodic.Interval} for next run of {worker}");
        Thread.Sleep(periodic.Interval);
        StartWorker(worker, RefreshTask(worker, periodic));
    });

    private static Action RefreshTask(BaseWorker worker, IPeriodic periodic) => () =>
    {
        if (worker.State < WorkerExecutionState.Created) //Failed
        {
            Log.Debug($"Task {worker} failed. Not refreshing.");
            return;
        } 
        Log.Debug($"Refreshing {worker}");
        Task periodicTask = RefreshedPeriodicTask(worker, periodic);
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
    
    internal static void StartWorker(BaseWorker worker, Action? finishedCallback = null)
    {
        Log.Debug($"Starting {worker}");
        if (ServiceProvider is null)
        {
            Log.Fatal("ServiceProvider is null");
            return;
        }
        Action afterWorkCallback = DefaultAfterWork(worker, finishedCallback);

        while (RunningWorkers.Count > Settings.MaxConcurrentWorkers)
        {
            Log.Warn($"{worker}: Max worker concurrency reached ({Settings.MaxConcurrentWorkers})! Waiting {Settings.WorkCycleTimeoutMs}ms...");
            Thread.Sleep(Settings.WorkCycleTimeoutMs);
        }
        
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

    private static Action DefaultAfterWork(BaseWorker worker, Action? callback = null) => () =>
    {
        Log.Debug($"DefaultAfterWork {worker}");
        try
        {
            if (RunningWorkers.TryGetValue(worker, out Task<BaseWorker[]>? task))
            {
                Log.Debug($"Waiting for Children to exit {worker}");
                task.Wait();
                if (task.IsCompleted)
                {
                    Log.Debug($"Children done {worker}");
                    BaseWorker[] newWorkers = task.Result;
                    Log.Debug($"{worker} created {newWorkers.Length} new Workers.");
                    AddWorkers(newWorkers);
                }else
                    Log.Warn($"Children failed: {worker}");
            }
            RunningWorkers.Remove(worker, out _);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
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
    
    internal static async Task<bool> AddMangaToContext(this MangaContext context, (Manga, MangaConnectorId<Manga>) addManga, CancellationToken token) =>
        await AddMangaToContext(context, addManga.Item1, addManga.Item2, token);

    internal static async Task<bool> AddMangaToContext(this MangaContext context, Manga addManga, MangaConnectorId<Manga> addMcId,
        CancellationToken token)
    {
        context.ChangeTracker.Clear();
        Log.Debug($"Adding Manga to Context: {addManga}");
        Manga? manga = await context.FindMangaLike(addManga, token);
        if (manga is not null)
        {
            Log.Debug($"Merging with existing Manga: {manga}");
            foreach (MangaConnectorId<Manga> mcId in addManga.MangaConnectorIds)
            {
                mcId.Obj = manga;
                mcId.ObjId = manga.Key;
            }
            manga.MangaTags = manga.MangaTags.UnionBy(addManga.MangaTags, tag => tag.Tag).ToList();
            manga.Authors = manga.Authors.UnionBy(addManga.Authors, author => author.Key).ToList();
            manga.Links = manga.Links.UnionBy(addManga.Links, link => link.Key).ToList();
            manga.AltTitles = manga.AltTitles.UnionBy(addManga.AltTitles, altTitle => altTitle.Key).ToList();
            manga.Chapters = manga.Chapters.UnionBy(addManga.Chapters, chapter => chapter.Key).ToList();
            manga.MangaConnectorIds = manga.MangaConnectorIds.UnionBy(addManga.MangaConnectorIds, id => id.MangaConnectorName).ToList();
        }
        else
        {
            Log.Debug("Manga does not exist yet.");
            IEnumerable<MangaTag> mergedTags = addManga.MangaTags.Select(mt =>
            {
                MangaTag? inDb = context.Tags.Find(mt.Tag);
                return inDb ?? mt;
            });
            addManga.MangaTags = mergedTags.ToList();

            IEnumerable<Author> mergedAuthors = addManga.Authors.Select(ma =>
            {
                Author? inDb = context.Authors.Find(ma.Key);
                return inDb ?? ma;
            });
            addManga.Authors = mergedAuthors.ToList();
            
            context.Mangas.Add(addManga);
        }
        
        if (await context.Sync(token, reason: "AddMangaToContext") is { success: false })
            return false;

        DownloadCoverFromMangaconnectorWorker downloadCoverWorker = new (addMcId);
        AddWorker(downloadCoverWorker);
        
        return true;
    }
}
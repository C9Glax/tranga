using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using API.MangaConnectors;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using API.Schema.MangaContext.MetadataFetchers;
using API.Workers;
using API.Workers.MangaDownloadWorkers;
using API.Workers.PeriodicWorkers;
using API.Workers.PeriodicWorkers.MaintenanceWorkers;
using log4net;
using Microsoft.EntityFrameworkCore;

namespace API;

public static class Tranga
{
    
    internal static IServiceProvider? ServiceProvider { get; set; }
    
    private static readonly ILog Log = LogManager.GetLogger(typeof(Tranga));
    internal static readonly MetadataFetcher[] MetadataFetchers = [new MyAnimeList()];
    internal static readonly MangaConnector[] MangaConnectors = [new Global(), new AsuraComic(), new MangaDex(), new Mangaworld(), new WeebCentral()];
    internal static readonly TrangaSettings Settings = TrangaSettings.Load();
    
    // ReSharper disable MemberCanBePrivate.Global
    internal static readonly UpdateMetadataWorker UpdateMetadataWorker = new ();
    internal static readonly SendNotificationsWorker SendNotificationsWorker = new();
    internal static readonly UpdateChaptersDownloadedWorker UpdateChaptersDownloadedWorker = new();
    internal static readonly CheckForNewChaptersWorker CheckForNewChaptersWorker = new();
    internal static readonly CleanupMangaCoversWorker CleanupMangaCoversWorker = new();
    internal static readonly StartNewChapterDownloadsWorker StartNewChapterDownloadsWorker = new();
    internal static readonly RemoveOldNotificationsWorker RemoveOldNotificationsWorker = new();
    internal static readonly UpdateCoversWorker UpdateCoversWorker = new();
    internal static readonly CleanupMangaconnectorIdsWithoutConnector CleanupMangaconnectorIdsWithoutConnector = new();
    // ReSharper restore MemberCanBePrivate.Global

    internal static readonly RateLimitHandler RateLimitHandler = new();

    internal static void StartupTasks()
    {
        AddWorker(SendNotificationsWorker);
        AddWorker(CleanupMangaconnectorIdsWithoutConnector);
        AddWorker(CleanupMangaCoversWorker);
        
        if(Constants.UpdateChaptersDownloadedBeforeStarting)
            AddWorker(UpdateChaptersDownloadedWorker);
        
        Log.Info("Waiting for startup to complete...");
        while (RunningWorkers.Any(w => w.Key.State < WorkerExecutionState.Completed))
            Thread.Sleep(1000);
        Log.Info("Start complete!");
    }

    internal static void AddDefaultWorkers()
    {
        AddWorker(UpdateMetadataWorker);
        AddWorker(CheckForNewChaptersWorker);
        AddWorker(StartNewChapterDownloadsWorker);
        AddWorker(RemoveOldNotificationsWorker);
        AddWorker(UpdateCoversWorker);
        
        if(Constants.UpdateChaptersDownloadedBeforeStarting)
            AddWorker(UpdateChaptersDownloadedWorker);
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
        Log.DebugFormat("Adding Worker {0}", worker);
        KnownWorkers.Add(worker);
        if(worker is not IPeriodic)
            StartWorker(worker, RemoveFromKnownWorkers(worker));
        else
            StartWorker(worker);
        if(worker is IPeriodic periodic)
            AddPeriodicWorker(worker, periodic);
    }

    private static void AddPeriodicWorker(BaseWorker worker, IPeriodic periodic)
    {
        Log.DebugFormat("Adding Periodic {0}", worker);
        Task periodicTask = RefreshedPeriodicTask(worker, periodic);
        PeriodicWorkers.TryAdd((worker as IPeriodic)!, periodicTask);
        periodicTask.Start();
    }

    private static Task RefreshedPeriodicTask(BaseWorker worker, IPeriodic periodic) => new (() =>
    {
        Log.DebugFormat("Waiting {0} for next run of {1}", periodic.Interval, worker);
        Thread.Sleep(periodic.Interval);
        StartWorker(worker, RefreshTask(worker, periodic));
    });

    private static Action RefreshTask(BaseWorker worker, IPeriodic periodic) => () =>
    {
        if (worker.State < WorkerExecutionState.Created) //Failed
        {
            Log.DebugFormat("Task {0} failed. Not refreshing.", worker);
            return;
        } 
        Log.DebugFormat("Refreshing {0}", worker);
        Task periodicTask = RefreshedPeriodicTask(worker, periodic);
        PeriodicWorkers.AddOrUpdate((worker as IPeriodic)!, periodicTask, (_, _) => periodicTask);
        periodicTask.Start();
    };

    private static Action RemoveFromKnownWorkers(BaseWorker worker) => () =>
    {
        if (KnownWorkers.Contains(worker))
            KnownWorkers.Remove(worker);
    };
    
    public static void AddWorkers(IEnumerable<BaseWorker> workers)
    {
        foreach (BaseWorker baseWorker in workers)
            AddWorker(baseWorker);
    }

    private static readonly HashSet<BaseWorker> KnownWorkers = new();
    public static BaseWorker[] GetKnownWorkers() =>  KnownWorkers.ToArray();
    private static readonly ConcurrentDictionary<BaseWorker, Task<BaseWorker[]>> RunningWorkers = new();
    public static BaseWorker[] GetRunningWorkers() => RunningWorkers.Keys.ToArray();
    
    internal static void StartWorker(BaseWorker worker, Action? finishedCallback = null)
    {
        Log.DebugFormat("Starting {0}", worker);
        if (ServiceProvider is null)
        {
            Log.Fatal("ServiceProvider is null");
            return;
        }
        Action afterWorkCallback = DefaultAfterWork(worker, finishedCallback);

        while (RunningWorkers.Count > Settings.MaxConcurrentWorkers)
        {
            Log.WarnFormat("{0}: Max worker concurrency reached ({1})! Waiting {2}ms...", worker, Settings.MaxConcurrentWorkers, Settings.WorkCycleTimeoutMs);
            Thread.Sleep(Settings.WorkCycleTimeoutMs);
        }

        if (worker is BaseWorkerWithContexts withContexts)
        {
            RunningWorkers.TryAdd(withContexts, withContexts.DoWork(ServiceProvider.CreateScope(), afterWorkCallback));
        }
        else
        {
            RunningWorkers.TryAdd(worker, worker.DoWork(afterWorkCallback));
        }
    }

    private static Action DefaultAfterWork(BaseWorker worker, Action? callback = null) => () =>
    {
        Log.DebugFormat("DefaultAfterWork {0}", worker);
        try
        {
            if (RunningWorkers.TryGetValue(worker, out Task<BaseWorker[]>? task))
            {
                if (!task.IsCompleted)
                {
                    Log.DebugFormat("Waiting for Children to exit {0}", worker);
                    task.Wait();
                }
                if (task.IsCompletedSuccessfully)
                {
                    Log.DebugFormat("Children done {0}", worker);
                    BaseWorker[] newWorkers = task.Result;
                    Log.DebugFormat("{0} created {1} new Workers.", worker, newWorkers.Length);
                    AddWorkers(newWorkers);
                }else
                    Log.WarnFormat("Children failed: {0}", worker);
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
        Log.DebugFormat("Stopping {0}", worker);
        if(worker is IPeriodic periodicWorker)
            PeriodicWorkers.Remove(periodicWorker, out _);
        worker.Cancel();
        RunningWorkers.Remove(worker, out _);
    }
    
    internal static async Task<(Manga manga, MangaConnectorId<Manga> id)?> AddMangaToContext(this MangaContext context, (Manga, MangaConnectorId<Manga>) addManga, CancellationToken token) =>
        await AddMangaToContext(context, addManga.Item1, addManga.Item2, token);

    internal static async Task<(Manga manga, MangaConnectorId<Manga> id)?> AddMangaToContext(this MangaContext context, Manga addManga, MangaConnectorId<Manga> addMcId, CancellationToken token)
    {
        context.ChangeTracker.Clear();
        Log.DebugFormat("Adding Manga to Context: {0}", addManga);
        (Manga, MangaConnectorId<Manga>)? result;
        if (await context.FindMangaLike(addManga, token) is { } mangaId)
        {
            Manga manga = await context.MangaIncludeAll().FirstAsync(m => m.Key == mangaId, token);
            Log.DebugFormat("Merging with existing Manga: {0}", manga);

            // Check for existing MangaConnectorId to avoid duplicate key tracking conflict
            var existingMcId = manga.MangaConnectorIds
                .FirstOrDefault(id => id.MangaConnectorName == addMcId.MangaConnectorName 
                                      && id.IdOnConnectorSite == addMcId.IdOnConnectorSite);

            MangaConnectorId<Manga> mcIdToUse;
            if (existingMcId == null)
            {
                // Create new if not exists (matches original constructor signature)
                mcIdToUse = new MangaConnectorId<Manga>(manga, addMcId.MangaConnectorName, addMcId.IdOnConnectorSite, addMcId.WebsiteUrl, addMcId.UseForDownload);
                manga.MangaConnectorIds.Add(mcIdToUse);
                Log.DebugFormat("Added new MangaConnectorId for {0}", addMcId.MangaConnectorName);
            }
            else
            {
                // Reuse existing; recreate if URL changed (init-only safe via constructor)
                mcIdToUse = existingMcId;
                if (existingMcId.WebsiteUrl != addMcId.WebsiteUrl)
                {
                    // Recreate with constructor (sets init-only WebsiteUrl)
                    var updatedMcId = new MangaConnectorId<Manga>(manga, existingMcId.MangaConnectorName, existingMcId.IdOnConnectorSite, addMcId.WebsiteUrl, existingMcId.UseForDownload);
                    manga.MangaConnectorIds.Remove(existingMcId);
                    manga.MangaConnectorIds.Add(updatedMcId);
                    mcIdToUse = updatedMcId;
                    Log.DebugFormat("Updated/Recreated MangaConnectorId for {0} (URL changed)", addMcId.MangaConnectorName);
                }
            }
            
            result = (manga, mcIdToUse);
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
            result = (addManga, addMcId);
        }
        
        if (await context.Sync(token, reason: "AddMangaToContext") is { success: false })
            return null;

        DownloadCoverFromMangaconnectorWorker downloadCoverWorker = new (result.Value.Item2);
        AddWorker(downloadCoverWorker);
        
        return result;
    }
}

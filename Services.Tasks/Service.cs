using Common.Services.Events;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Services.Manga.Database;
using Services.Tasks.Database;
using Services.Tasks.EventHandlers;
using Services.Tasks.Features;
using Services.Tasks.Tasks;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;
using Constants = Common.Settings.Constants;

namespace Services.Tasks;

/// <summary>
/// Entry point and host setup for the <c>Services.Tasks</c> service, which owns task queueing, worker scaling,
/// and periodic/recurring jobs (chapter fetching, metadata updates, cleanup).
/// </summary>
public sealed class Service : Common.Services.Service
{
    private readonly List<IEventHandler> _eventHandlers = [];

    /// <summary>
    /// Configures the DB contexts, task queue/worker/scheduler services, event handlers, and runs pending EF
    /// migrations before seeding the default recurring tasks. Skips infra wiring (RabbitMQ, migrations, hosted
    /// services) when running under <see cref="Constants.OpenApiDocumentationRun"/>.
    /// </summary>
    /// <param name="args">Command-line arguments forwarded to the base <see cref="Common.Services.Service"/>.</param>
    public Service(string[] args) : base(args)
    {
        Builder.Services.AddDbContext<MangaContext>();
        Builder.Services.AddDbContext<TasksContext>();

        Builder.Services.AddScoped<EventPublisher>();

        if (!Constants.OpenApiDocumentationRun)
        {
            Builder.Services.AddSingleton<TaskQueue>();

            Builder.Services.AddHostedService<WorkerPool>();

            Builder.Services.AddHostedService<PeriodicTaskScheduler>();
        }

        SetupWebApplication<Endpoints>("/tasks");

        if (!Constants.OpenApiDocumentationRun)
            AddTrangaEventHandlers(App);

        if (!Constants.OpenApiDocumentationRun)
        {
            using MangaContext mangaContext = App.Services.CreateScope().ServiceProvider.GetRequiredService<MangaContext>();
            mangaContext.Database.MigrateAsync(CancellationToken.None).Wait();

            using TasksContext tasksContext = App.Services.CreateScope().ServiceProvider.GetRequiredService<TasksContext>();
            tasksContext.Database.MigrateAsync(CancellationToken.None).Wait();

            CreateDefaultTasks(App.Services.GetRequiredService<TaskQueue>(), CancellationToken.None).Wait();
        }
    }

    /// <summary>
    /// Registers this service's RabbitMQ event handlers (e.g. <see cref="DownloadLinkModifiedHandler"/>) against
    /// the app's <see cref="IChannel"/>, keeping references in <see cref="_eventHandlers"/> alive.
    /// </summary>
    /// <param name="app">The running web application whose service provider is used to resolve dependencies.</param>
    private  void AddTrangaEventHandlers(WebApplication app)
    {
        IChannel channel = app.Services.GetRequiredService<IChannel>();
        _eventHandlers.Add(new DownloadLinkModifiedHandler(channel, app.Services));
    }

    /// <summary>
    /// Registers the built-in periodic tasks (<see cref="DbFileCleanupTask"/>, <see cref="MissingChapterScanTask"/>,
    /// <see cref="PeriodicMangaChapterFetcherTask"/>) with <see cref="TasksCollection"/> and enqueues every known
    /// task onto the given queue.
    /// </summary>
    /// <param name="taskQueue">The queue that default tasks are added to.</param>
    /// <param name="ct">Token used to cancel the enqueue operations.</param>
    private async Task CreateDefaultTasks(TaskQueue taskQueue, CancellationToken ct)
    {
        App.Logger.LogDebug("Adding default tasks...");
        TasksCollection.PeriodicTasks.Add(new DbFileCleanupTask());
        TasksCollection.PeriodicTasks.Add(new MissingChapterScanTask());
        TasksCollection.PeriodicTasks.Add(new PeriodicMangaChapterFetcherTask());
        try
        {
            foreach (TaskBase task in TasksCollection.GetKnownTasks())
            {
                await taskQueue.AddTaskToQueue(task, ct);
            }
        }
        catch (Exception)
        {
            // Probably build
        }
    }

    /// <summary>Process entry point: constructs the <see cref="Service"/> and runs it to completion.</summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        Service service = new (args);
        Task.WaitAll(service.Run());
    }
}
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

public sealed class Service : Common.Services.Service
{
    private readonly List<IEventHandler> _eventHandlers = [];
    
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

    private  void AddTrangaEventHandlers(WebApplication app)
    {
        IChannel channel = app.Services.GetRequiredService<IChannel>();
        _eventHandlers.Add(new DownloadLinkModifiedHandler(channel, app.Services));
    }

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

    public static void Main(string[] args)
    {
        Service service = new (args);
        Task.WaitAll(service.Run());
    }
}
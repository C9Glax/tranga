using Common.Database;
using Services.Manga.Database;
using Services.Tasks.Features;
using Services.Tasks.Tasks;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;
using Settings;

namespace Services.Tasks;

public sealed class Service : Common.Services.Service
{
    public Service(string[] args) : base(args)
    {
        Builder.Services.AddDbContext<MangaContext>(opts =>
            opts.Configure(DatabaseContextOptionsBuilder.DbType.Postgresql));
        
        if (!Constants.OpenApiDocumentationRun)
        {
            Builder.Services.AddSingleton<TaskQueue>();

            for (int i = 0; i < EnvVars.WorkersCount; i++)
                Builder.Services.AddHostedService<TaskWorker>();

            Builder.Services.AddHostedService<PeriodicTaskScheduler>();
        }
        
        SetupWebApplication<Endpoints>("/tasks");

        if (!Constants.OpenApiDocumentationRun)
        {
            using MangaContext mangaContext = App.Services.CreateScope().ServiceProvider.GetRequiredService<MangaContext>();
            Task.WaitAll(mangaContext.ApplyMigrations());

            CreateDefaultTasks(App.Services.GetRequiredService<TaskQueue>(), CancellationToken.None).Wait();
        }
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
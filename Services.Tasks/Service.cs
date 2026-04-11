using Database;
using Database.MangaContext;
using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;
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
        Builder.Services.AddDbContext<Context>(opts =>
            opts.Configure(DatabaseContextOptionsBuilder.DbType.Postgresql));
        
        Builder.Services.AddDbContext<MangaContext>(opts =>
            opts.Configure(DatabaseContextOptionsBuilder.DbType.Postgresql));

        Builder.Services.AddSingleton<TaskQueue>();

        for (int i = 0; i < EnvVars.WorkersCount; i++)
            Builder.Services.AddScoped<TaskWorker>();

        Builder.Services.AddScoped<PeriodicTaskScheduler>();
        
        SetupWebApplication<Endpoints>();
        
        using MangaContext mangaContext = App.Services.CreateScope().ServiceProvider.GetRequiredService<MangaContext>();
        Task.WaitAll(mangaContext.ApplyMigrations());
        
        using Context context = App.Services.CreateScope().ServiceProvider.GetRequiredService<Context>();
        Task.WaitAll(context.ApplyMigrations());
        CreateDefaultTasks(context).Wait();
    }

    private async Task CreateDefaultTasks(Context context)
    {
        App.Logger.LogDebug("Adding default tasks...");
        TaskBase[] defaultTasks =
        [
            new DbFileCleanupTask(Guid.CreateVersion7())
        ];
        try
        {
            List<Guid> existingTaskTypeIds = await context.Tasks.Select(t => t.TaskTypeId).ToListAsync();
            IEnumerable<DbTask> newTasks = defaultTasks.Where(t => !existingTaskTypeIds.Contains(t.TaskTypeId)).Select(t => t.CreateDbTaskFromTask());
            await context.Tasks.AddRangeAsync(newTasks);
            await context.SaveChangesAsync();
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
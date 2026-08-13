using Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Services.Manga.Database;
using Services.Tasks.Tasks;
using Services.Tasks.Tests.Helpers;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tests.Tasks;

public class PeriodicMangaChapterFetcherTaskTests : TrangaTest
{
    [Fact]
    public async Task RunAsync_QueuesGetMangaChaptersTask_OnlyForMonitoredMangas()
    {
        IServiceProvider services = BuildServices();
        Guid monitoredMangaId = Guid.CreateVersion7();
        Guid unmonitoredMangaId = Guid.CreateVersion7();

        using (IServiceScope seedScope = services.CreateScope())
        {
            MangaContext seedCtx = seedScope.ServiceProvider.GetRequiredService<MangaContext>();
            await seedCtx.Mangas.AddRangeAsync(
            [
                new DbManga { MangaId = monitoredMangaId, Monitored = true },
                new DbManga { MangaId = unmonitoredMangaId, Monitored = false }
            ], ct);
            await seedCtx.SaveChangesAsync(ct);
        }

        PeriodicMangaChapterFetcherTask task = new();
        try
        {
            using IServiceScope scope = services.CreateScope();
            await task.ExecuteAsync(scope, NoOpLogger.Instance, ct);

            GetMangaChaptersTask[] created = TasksCollection.RunOnceTasks.Values
                .OfType<GetMangaChaptersTask>()
                .Where(t => t.MangaId == monitoredMangaId || t.MangaId == unmonitoredMangaId)
                .ToArray();

            GetMangaChaptersTask queuedTask = Assert.Single(created);
            Assert.Equal(monitoredMangaId, queuedTask.MangaId);
        }
        finally
        {
            foreach (Guid taskId in TasksCollection.RunOnceTasks.Values
                         .OfType<GetMangaChaptersTask>()
                         .Where(t => t.MangaId == monitoredMangaId || t.MangaId == unmonitoredMangaId)
                         .Select(t => t.TaskId)
                         .ToArray())
            {
                TasksCollection.RunOnceTasks.TryRemove(taskId, out _);
            }
        }
    }

    private static IServiceProvider BuildServices()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), "TrangaTests", "Services.Tasks.Tests.MangaContext", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        ServiceCollection services = new();
        services.AddDbContext<MangaContext>(options => options.UseSqlite($"Data Source={dbPath}"));
        ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<MangaContext>().Database.EnsureCreated();

        return provider;
    }
}

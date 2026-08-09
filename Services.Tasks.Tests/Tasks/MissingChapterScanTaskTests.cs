using Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Services.Manga.Database;
using Services.Tasks.Tasks;
using Services.Tasks.Tests.Helpers;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tests.Tasks;

public class MissingChapterScanTaskTests : TrangaTest
{
    [Fact]
    public async Task RunAsync_OrdersDownloadTasksByChapterNumber_WithinSameManga()
    {
        IServiceProvider services = BuildServices();
        Guid mangaId = Guid.CreateVersion7();
        Guid chapter1Id, chapter2Id, chapter10Id;

        using (IServiceScope seedScope = services.CreateScope())
        {
            MangaContext seedCtx = seedScope.ServiceProvider.GetRequiredService<MangaContext>();
            await seedCtx.Mangas.AddAsync(new DbManga { MangaId = mangaId, Monitored = true }, ct);

            // Seeded out of numeric order, and "10" sorts before "2" lexicographically,
            // to make sure ordering is numeric rather than a leftover string sort.
            DbChapter chapter10 = new() { MangaId = mangaId, Number = "10" };
            DbChapter chapter2 = new() { MangaId = mangaId, Number = "2" };
            DbChapter chapter1 = new() { MangaId = mangaId, Number = "1" };
            await seedCtx.Chapters.AddRangeAsync([chapter10, chapter2, chapter1], ct);
            await seedCtx.SaveChangesAsync(ct);

            chapter1Id = chapter1.ChapterId;
            chapter2Id = chapter2.ChapterId;
            chapter10Id = chapter10.ChapterId;
        }

        MissingChapterScanTask task = new();
        try
        {
            using IServiceScope scope = services.CreateScope();
            await task.ExecuteAsync(scope, NoOpLogger.Instance, ct);

            DownloadChapterTask[] created = TasksCollection.RunOnceTasks.Values
                .OfType<DownloadChapterTask>()
                .Where(t => t.MangaId == mangaId)
                .ToArray();

            Assert.Equal(3, created.Length);

            DownloadChapterTask chapter1Task = Assert.Single(created, t => t.ChapterId == chapter1Id);
            DownloadChapterTask chapter2Task = Assert.Single(created, t => t.ChapterId == chapter2Id);
            DownloadChapterTask chapter10Task = Assert.Single(created, t => t.ChapterId == chapter10Id);

            Assert.True(chapter1Task.Priority < chapter2Task.Priority);
            Assert.True(chapter2Task.Priority < chapter10Task.Priority);
        }
        finally
        {
            foreach (Guid taskId in TasksCollection.RunOnceTasks.Values
                         .OfType<DownloadChapterTask>()
                         .Where(t => t.MangaId == mangaId)
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

using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tasks;

/// <summary>
/// Creates <see cref="GetMangaChaptersTask"/>s for all <see cref="DbManga"/> with <see cref="DbManga.Monitored"/>
/// </summary>
internal sealed class PeriodicMangaChapterFetcherTask() : PeriodicTask(Guid.Parse("4e9e2910-a49d-4282-894a-1a847f0f344b"))
{
    internal override TimeSpan Interval { get; init; } = TimeSpan.FromHours(3);
    
    private MangaContext _ctx = null!;
    
    private protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        logger.LogDebug("Getting monitored Mangas...");
        if (await _ctx.Mangas.Where(m => m.Monitored).Select(m => m.MangaId).ToListAsync(stoppingToken) is not { } list)
            return;
        logger.LogDebug("Got {list.Count} Mangas.", list.Count);

        IEnumerable<GetMangaChaptersTask> newTasks = list.Select(m => new GetMangaChaptersTask(m));
        foreach (GetMangaChaptersTask task in newTasks)
        {
            if (TasksCollection.RunOnceTasks.Values.OfType<GetMangaChaptersTask>().All(t => t.MangaId != task.MangaId))
            {
                logger.LogDebug("Adding {nameof(GetMangaChaptersTask)} for Manga {task.MangaId}", nameof(GetMangaChaptersTask), task.MangaId);
                TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);
            }
            else
            {
                logger.LogTrace("Manga {task.MangaId} already has {nameof(GetMangaChaptersTask)} Task.", task.MangaId, nameof(GetMangaChaptersTask));
            }
        }
    }
    
    private protected override void RefreshScope(IServiceScope scope)
    {
        _ctx = scope.ServiceProvider.GetRequiredService<MangaContext>();
    }
}
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tasks;

/// <summary>
/// Creates <see cref="DownloadChapterTask"/>s for all <see cref="DbChapter"/> that do not have a <see cref="DbChapterDownloadLink"/> with a <see cref="DbChapterDownloadLink.FileId"/>
/// </summary>
internal sealed class MissingChapterScanTask() : PeriodicTask(Guid.Parse("9a9e9232-98f5-4d0b-9e49-30da28c6d303"))
{
    internal override TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(15);

    private MangaContext _ctx = null!;
    
    private protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        // List of Chapters that already have a DownloadChapterTask
        IEnumerable<Guid> chapterIds = TasksCollection.RunOnceTasks.Values.OfType<DownloadChapterTask>().Select(t => t.ChapterId);

        var chaptersWithoutFiles = await _ctx.Chapters.Include(c => c.DownloadLinks)
            .Where(c => !chapterIds.Contains(c.ChapterId) && c.DownloadLinks!.All(d => d.FileId == null))
            .OrderBy(c => c.Number)
            .Select(c => new { MangaId = c.MangaId, ChapterId = c.ChapterId })
            .GroupBy(c => c.MangaId)
            .ToListAsync(stoppingToken);

        int priority = 0;
        foreach (var manga in chaptersWithoutFiles)
        {
            priority++;
            DownloadChapterTask[] tasks = manga.Select(t => new DownloadChapterTask(t.MangaId, t.ChapterId)
            {
                Priority = priority
            }).ToArray();
            foreach (DownloadChapterTask task in tasks)
            {
                logger.LogDebug("Adding {nameof(DownloadChapterTask)} for Chapter {task.ChapterId}", nameof(DownloadChapterTask), task.ChapterId);
                TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);
            }
        }
    }

    private protected override void RefreshScope(IServiceScope scope)
    {
        _ctx = scope.ServiceProvider.GetRequiredService<MangaContext>();
    }
}
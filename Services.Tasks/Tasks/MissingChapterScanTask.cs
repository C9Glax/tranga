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
    
    /// <summary>
    /// Number of Priority values reserved per Manga, so that chapters of one Manga can be ordered by
    /// <see cref="ParseChapterNumber"/> amongst themselves without overlapping into the next Manga's range.
    /// </summary>
    private const int MangaPriorityStep = 100_000;

    protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        // List of Chapters that already have a non-terminal DownloadChapterTask (a completed/failed one should not block a retry)
        IEnumerable<Guid> chapterIds = TasksCollection.RunOnceTasks.Values.OfType<DownloadChapterTask>()
            .Where(t => t.Status is not (TaskState.Completed or TaskState.Failed))
            .Select(t => t.ChapterId);

        logger.LogDebug("Scanning for Chapters without downloaded {nameof(DbFile)}...", nameof(DbFile));
        var chaptersWithoutFiles = await _ctx.Chapters.Include(c => c.DownloadLinks)
            .Where(c => !chapterIds.Contains(c.ChapterId) && c.DownloadLinks!.All(d => d.FileId == null))
            .Select(c => new { MangaId = c.MangaId, ChapterId = c.ChapterId, Number = c.Number })
            .GroupBy(c => c.MangaId)
            .ToListAsync(stoppingToken);
        logger.LogDebug("Found {chaptersWithoutFiles.Count} Mangas with missing Chapters.", chaptersWithoutFiles.Count);

        int mangaIndex = 0;
        int totalTasksAdded = 0;
        foreach (var manga in chaptersWithoutFiles)
        {
            mangaIndex++;
            DownloadChapterTask[] tasks = manga
                .OrderBy(t => ParseChapterNumber(t.Number))
                .Select((t, chapterIndex) => new DownloadChapterTask(t.MangaId, t.ChapterId)
                {
                    Priority = mangaIndex * MangaPriorityStep + chapterIndex
                }).ToArray();
            foreach (DownloadChapterTask task in tasks)
            {
                logger.LogDebug("Adding {nameof(DownloadChapterTask)} for Chapter {task.ChapterId}", nameof(DownloadChapterTask), task.ChapterId);
                TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);
                totalTasksAdded++;
            }
        }
        if (totalTasksAdded > 0)
            logger.LogInformation("Queued {totalTasksAdded} {nameof(DownloadChapterTask)} across {chaptersWithoutFiles.Count} Mangas.",
                totalTasksAdded, nameof(DownloadChapterTask), chaptersWithoutFiles.Count);
    }

    /// <summary>
    /// Parses <see cref="DbChapter.Number"/> (e.g. "10", "10.5") into a numeric value for ordering.
    /// Chapter numbers that cannot be parsed sort last.
    /// </summary>
    private static double ParseChapterNumber(string number) =>
        double.TryParse(number, out double parsed) ? parsed : double.MaxValue;

    protected override void RefreshScope(IServiceScope scope)
    {
        _ctx = scope.ServiceProvider.GetRequiredService<MangaContext>();
    }
}
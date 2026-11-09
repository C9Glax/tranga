using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Tasks;

/// <summary>
/// Removes <see cref="DbFile"/> from the Database that do no longer have an associated Cover or Chapter
/// </summary>
internal sealed class DbFileCleanupTask() : PeriodicTask(Guid.Parse("ded1e7d1-ec8e-4795-910a-80bdd0d797d5"))
{
    internal override TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    private MangaContext _ctx = null!;

    /// <summary>
    /// Removes DbFiles from Database that have no DbChapterDownloadLink, DbMetadata, DbDownloadLink linked to them 
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="logger"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        logger.LogDebug("Searching for orphaned {nameof(DbFile)}...", nameof(DbFile));
        int amount = await _ctx.Files.Where(f =>
            !_ctx.ChapterDownloadLinks.Any(c => c.FileId == f.FileId) &&
            !_ctx.MetadataEntries.Any(m => m.CoverId == f.FileId) &&
            !_ctx.DownloadLinks.Any(d => d.CoverId == f.FileId)
        ).ExecuteDeleteAsync(stoppingToken);
        if (amount > 0)
            logger.LogInformation("Removed {amount} orphaned {nameof(DbFile)}", amount, nameof(DbFile));
        else
            logger.LogDebug("No orphaned {nameof(DbFile)} found.", nameof(DbFile));
    }

    protected override void RefreshScope(IServiceScope scope)
    {
        _ctx = scope.ServiceProvider.GetRequiredService<MangaContext>();
    }
}
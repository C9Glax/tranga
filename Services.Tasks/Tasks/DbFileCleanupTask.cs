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
    private protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        int amount = await _ctx.Files.Where(f =>
            _ctx.ChapterDownloadLinks.Select(c => c.FileId).Union(_ctx.MetadataEntries.Select(m => m.CoverId))
                .Union(_ctx.DownloadLinks.Select(d => d.CoverId))
                .Any(i => i == f.FileId)
        ).ExecuteDeleteAsync(stoppingToken);
        logger.LogDebug("Removed {amount} {nameof(DbFile)}", amount, nameof(DbFile));
    }

    private protected override void RefreshScope(IServiceScope scope)
    {
        _ctx = scope.ServiceProvider.GetRequiredService<MangaContext>();
    }
}
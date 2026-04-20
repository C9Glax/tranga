using Extensions;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Helpers;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Tasks;

/// <summary>
/// Retrieves the <see cref="DbChapterDownloadLink"/> from the <see cref="IDownloadExtension"/> with the highest Priority for the <see cref="DbManga"/>
/// </summary>
/// <param name="mangaId">ID of the manga</param>
internal sealed class GetMangaChaptersTask(Guid mangaId) : RunOnceTask(Guid.Parse("571a5bad-d955-4bf0-b75d-d1dcf54f8e69")), IMangaTask
{
    public Guid MangaId { get; init; } = mangaId;
    
    private MangaContext _ctx = null!;
    
    private protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        if (await _ctx.Mangas.Include(m => m.DownloadLinks).SingleOrDefaultAsync(m => m.MangaId == MangaId, stoppingToken) is not { } manga)
        {
            logger.LogError("Could not find Manga {MangaId}", MangaId);
            return;
        }
        
        if (manga.DownloadLinks!.Where(d => d.Matched).MinBy(d => d.Priority)?.DownloadLink is not { } link)
        {
            logger.LogDebug("No Matched DownloadLink");
            return;
        }
        logger.LogDebug("Got {link.DownloadExtension} {link.Identifier}.", link.DownloadExtension, link.Identifier);

        if (DownloadExtensionsCollection.GetExtension(link.DownloadExtension) is not { } extension)
        {
            logger.LogError("Could not find {nameof(IDownloadExtension)} {link.DownloadExtension}",
                nameof(IDownloadExtension), link.DownloadExtension);
            return;
        }

        logger.LogDebug("Getting chapters...");
        if (await extension.GetChapters(link.ToMangaInfo(), stoppingToken) is not { } chapters)
        {
            logger.LogError("Could not get chapters!");
            return;
        }
        logger.LogDebug("Got {chapters.Count} chapters...", chapters.Count);

        DbChapter[] newChapters = chapters.Select(c => c.ToChapter(manga).CreateAndAddChapterDownloadLink(c)).ToArray();
        foreach (DbChapterDownloadLink downloadLink in newChapters.SelectMany(c => c.DownloadLinks!))
        {
            logger.LogTrace($"Adding {nameof(DbChapter)} and/or {nameof(DbChapterDownloadLink)}...");
            if (await _ctx.ChapterDownloadLinks.AnyAsync(
                    d => downloadLink.DownloadExtension == d.DownloadExtension &&
                         downloadLink.Identifier == d.Identifier, stoppingToken))
            {
                logger.LogTrace(
                    "{nameof(DbChapterDownloadLink)} {downloadLink.DownloadExtension} {downloadLink.Identifier} already exists.",
                    nameof(DbChapterDownloadLink), downloadLink.DownloadExtension, downloadLink.Identifier);
                continue;
            }

            // Insert DownloadLinks into chapters that already exist
            if (await _ctx.Chapters.FirstOrDefaultAsync(
                    c => c.MangaId == manga.MangaId && downloadLink.Chapter!.Number == c.Number, stoppingToken) is
                { } dbChapter)
            {
                DbChapterDownloadLink newLink = downloadLink with
                {
                    ChapterId = dbChapter.ChapterId,
                    Chapter = dbChapter
                };
                await _ctx.AddAsync(newLink, stoppingToken);
                logger.LogDebug("Added {nameof(DbChapterDownloadLink)} to {dbChapter.ChapterId}",
                    nameof(DbChapterDownloadLink), dbChapter.ChapterId);
            }
            else
            {
                await _ctx.AddAsync(downloadLink, stoppingToken);
                logger.LogDebug("Added {nameof(DbChapter)} {chapter.ChapterId}", nameof(DbChapter),
                    downloadLink.ChapterId);
            }
        }

        await _ctx.SaveChangesAsync(stoppingToken);
    }

    private protected override void RefreshScope(IServiceScope scope)
    {
        _ctx = scope.ServiceProvider.GetRequiredService<MangaContext>();
    }

    public override string ToString() => $"{base.ToString()} - Manga {MangaId}";
}
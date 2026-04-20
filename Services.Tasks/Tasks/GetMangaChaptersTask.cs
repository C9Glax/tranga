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
internal sealed class GetMangaChaptersTask(Guid mangaId) : RunOnceTask(Guid.Parse("571a5bad-d955-4bf0-b75d-d1dcf54f8e69"))
{
    internal Guid MangaId { get; init; } = mangaId;
    
    private MangaContext _ctx = null!;
    
    private protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        if (await _ctx.Mangas.Include(m => m.DownloadLinks).SingleOrDefaultAsync(m => m.MangaId == MangaId, stoppingToken) is not { } manga)
            return;
        if (manga.DownloadLinks!.Where(d => d.Matched).MinBy(d => d.Priority)?.DownloadLink is not { } link)
            return;
        logger.LogDebug("Got {link.DownloadExtension} {link.Identifier}.", link.DownloadExtension, link.Identifier);
        if (DownloadExtensionsCollection.GetExtension(link.DownloadExtension) is not { } extension)
            return;
        logger.LogDebug("Getting chapters...");
        if (await extension.GetChapters(link.ToMangaInfo(), stoppingToken) is not { } chapters)
            return;
        logger.LogDebug("Got {chapters.Count} chapters...", chapters.Count);

        IEnumerable<DbChapter> newChapters = chapters.Select(c => c.ToChapter(manga).CreateAndAddChapterDownloadLink(c));
        // Filter DownloadLinks that already exist
        string[] existingDownloadLinks = await _ctx.ChapterDownloadLinks
            .Where(dbDl => newChapters.SelectMany(c => c.DownloadLinks!).Any(dl =>
                dbDl.DownloadExtension == dl.DownloadExtension && dbDl.Identifier == dl.Identifier))
            .Select(dbDl => dbDl.Identifier).ToArrayAsync(stoppingToken);
        DbChapter[] dbChapters = newChapters.Where(c => !c.DownloadLinks!.Any(dl => existingDownloadLinks.Contains(dl.Identifier))).ToArray();
        logger.LogDebug("After removing duplicates {dbChapters.Count} chapters...", dbChapters.Length);
        
        foreach (DbChapter chapter in dbChapters)
        {
            logger.LogTrace($"Adding {nameof(DbChapter)} or {nameof(DbChapterDownloadLink)}...");
            // Insert DownloadLinks into chapters that already exist
            if(await _ctx.Chapters.FirstOrDefaultAsync(c => c.MangaId == manga.MangaId && chapter.Number == c.Number, stoppingToken) is { } dbChapter)
            {
                DbChapterDownloadLink[] dbChapterDownloadLinks = chapter.DownloadLinks!.ToArray();
                foreach (DbChapterDownloadLink l in dbChapterDownloadLinks)
                {
                    DbChapterDownloadLink newLink = l with
                    {
                        ChapterId = dbChapter.ChapterId,
                        Chapter = dbChapter
                    };
                    await _ctx.AddAsync(newLink, stoppingToken);
                }
                logger.LogDebug("Added {nameof(DbChapterDownloadLink)} to {dbChapter.ChapterId}", nameof(DbChapterDownloadLink), dbChapter.ChapterId);
            }
            else
            {
                await _ctx.AddAsync(chapter, stoppingToken);
                logger.LogDebug("Added {nameof(DbChapter)} {chapter.ChapterId}", nameof(DbChapter), chapter.ChapterId);
            }
        }
    }

    private protected override void RefreshScope(IServiceScope scope)
    {
        _ctx = scope.ServiceProvider.GetRequiredService<MangaContext>();
    }

    public override string ToString() => $"{base.ToString()} - Manga {MangaId}";
}
using Extensions;
using Extensions.Data;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Helpers;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Tasks;

/// <summary>
/// Retrieves the <see cref="DbChapterDownloadLink"/> from the <see cref="IDownloadExtension"/> with the highest Priority for the <see cref="DbManga"/>
/// </summary>
/// <param name="mangaId">ID of the manga</param>
internal sealed class GetMangaChaptersTask(Guid mangaId) : RunOnceTask(GetMangaChaptersTask.TaskTypeIdValue), IMangaTask
{
    internal static readonly Guid TaskTypeIdValue = Guid.Parse("571a5bad-d955-4bf0-b75d-d1dcf54f8e69");

    public Guid MangaId { get; init; } = mangaId;
    
    private MangaContext _ctx = null!;
    
    protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        logger.LogDebug("Fetching chapters for Manga {MangaId}...", MangaId);
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
        logger.LogDebug("Got {DownloadExtension} {Identifier}.", link.DownloadExtension, link.Identifier);

        if (DownloadExtensionsCollection.GetExtension(link.DownloadExtension) is not { } extension)
        {
            logger.LogError("Could not find {ExtensionType} {DownloadExtension}",
                nameof(IDownloadExtension), link.DownloadExtension);
            return;
        }

        logger.LogDebug("Getting chapters...");
        if (await extension.GetChapters(link.ToMangaInfo(), stoppingToken) is not { } chapters)
        {
            logger.LogError("Could not get chapters!");
            return;
        }
        logger.LogDebug("Got {ChapterCount} chapters...", chapters.Count);

        (int addedChapters, int addedLinks, int skippedExisting) = await MergeChaptersAsync(_ctx, manga, chapters, logger, stoppingToken);

        try
        {
            await _ctx.SaveChangesAsync(stoppingToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e,
                "Manga {MangaId}: failed saving {addedChapters} new Chapters and {addedLinks} new DownloadLinks.",
                MangaId, addedChapters, addedLinks);
            throw;
        }

        logger.LogInformation(
            "Manga {MangaId}: added {addedChapters} new Chapters, {addedLinks} new DownloadLinks, skipped {skippedExisting} existing.",
            MangaId, addedChapters, addedLinks, skippedExisting);
    }

    /// <summary>
    /// Adds the given <paramref name="chapters"/> to the <paramref name="ctx"/> (without saving).
    /// A <see cref="DbChapterDownloadLink"/> is attached to an existing/previously added <see cref="DbChapter"/> with the same
    /// Number only if that chapter has no link for the same extension yet (the key is ChapterId + DownloadExtension);
    /// otherwise (e.g. extras without a number that share a fallback number) a separate <see cref="DbChapter"/> is created.
    /// </summary>
    internal static async Task<(int AddedChapters, int AddedLinks, int SkippedExisting)> MergeChaptersAsync(
        MangaContext ctx, DbManga manga, IEnumerable<ChapterInfo> chapters, ILogger logger, CancellationToken ct)
    {
        int addedLinks = 0, addedChapters = 0, skippedExisting = 0;
        // Chapters added during this run are not visible to database queries yet.
        List<DbChapter> batch = [];
        HashSet<(Guid Extension, string Identifier)> batchLinks = [];

        foreach (ChapterInfo info in chapters)
        {
            logger.LogTrace("Processing chapter {Number} '{Title}' ({Identifier}) from {Extension}...", info.Number,
                info.Title, info.Identifier, info.ExtensionIdentifier);
            if (batchLinks.Contains((info.ExtensionIdentifier, info.Identifier)) ||
                await ctx.ChapterDownloadLinks.AnyAsync(
                    d => d.DownloadExtension == info.ExtensionIdentifier && d.Identifier == info.Identifier, ct))
            {
                logger.LogTrace("DownloadLink {Extension} {Identifier} already exists (chapter {Number}).",
                    info.ExtensionIdentifier, info.Identifier, info.Number);
                skippedExisting++;
                continue;
            }
            batchLinks.Add((info.ExtensionIdentifier, info.Identifier));

            // Insert DownloadLinks into chapters that already exist and do not have a link for this extension yet
            DbChapter? target = batch.FirstOrDefault(c =>
                c.Number == info.Number && c.DownloadLinks!.All(l => l.DownloadExtension != info.ExtensionIdentifier));
            target ??= await ctx.Chapters.FirstOrDefaultAsync(c =>
                c.MangaId == manga.MangaId && c.Number == info.Number &&
                !c.DownloadLinks!.Any(l => l.DownloadExtension == info.ExtensionIdentifier), ct);

            if (target is not null)
            {
                DbChapterDownloadLink newLink = info.ToChapterDownloadLink(target);
                await ctx.AddAsync(newLink, ct);
                logger.LogDebug("Added DownloadLink {Identifier} to Chapter {ChapterId} (Number {Number}).",
                    info.Identifier, target.ChapterId, info.Number);
                addedLinks++;
            }
            else
            {
                DbChapter chapter = info.ToChapter(manga).CreateAndAddChapterDownloadLink(info);
                await ctx.AddAsync(chapter, ct);
                batch.Add(chapter);
                logger.LogDebug("Added Chapter {ChapterId} (Number {Number}, Title '{Title}') with DownloadLink {Identifier}.",
                    chapter.ChapterId, info.Number, info.Title, info.Identifier);
                addedChapters++;
            }
        }

        return (addedChapters, addedLinks, skippedExisting);
    }

    protected override void RefreshScope(IServiceScope scope)
    {
        _ctx = scope.ServiceProvider.GetRequiredService<MangaContext>();
    }

    public override string ToString() => $"{base.ToString()} - Manga {MangaId}";
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Services.Manga.Database.Helpers;

/// <summary>
/// Merges one Manga's data into another.
/// </summary>
public static class MangaMergeHelper
{
    /// <summary>
    /// Merges <paramref name="sourceMangaId"/> into <paramref name="targetMangaId"/> and deletes the source Manga.
    /// Metadata candidates and Download-Link matches from both Manga are combined onto the target; which side's
    /// Chosen metadata entry and Chapters survive is controlled by <paramref name="keepSourceMetadata"/> and
    /// <paramref name="keepSourceChapters"/> respectively. The discarded side's Chapters are deleted (either
    /// explicitly, if the target's Chapters are discarded, or via cascade delete when the source Manga row is
    /// removed at the end of this method).
    /// </summary>
    /// <param name="mangaContext">The database context to operate on.</param>
    /// <param name="targetMangaId">Id of the Manga that survives the merge.</param>
    /// <param name="sourceMangaId">Id of the Manga being merged away and deleted.</param>
    /// <param name="keepSourceMetadata">Whether the source's chosen Metadata-Entry (Title/Summary/Cover) should become the target's chosen entry, instead of the target's own.</param>
    /// <param name="keepSourceChapters">Whether the source's Chapters should replace the target's Chapters, instead of being discarded.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task MergeInto(this MangaContext mangaContext, Guid targetMangaId, Guid sourceMangaId,
        bool keepSourceMetadata, bool keepSourceChapters, CancellationToken ct)
    {
        await using IDbContextTransaction transaction = await mangaContext.Database.BeginTransactionAsync(ct);

        await MergeMetadata(mangaContext, targetMangaId, sourceMangaId, keepSourceMetadata, ct);
        await MergeDownloadLinks(mangaContext, targetMangaId, sourceMangaId, ct);
        await MergeChapters(mangaContext, targetMangaId, sourceMangaId, keepSourceChapters, ct);

        // Cascade-deletes the source's remaining Chapters/Chapter-Download-Links (if its Chapters were not
        // kept above); its Metadata/Download-Link join rows were already re-pointed onto the target.
        await mangaContext.Mangas.Where(m => m.MangaId == sourceMangaId).ExecuteDeleteAsync(ct);

        await transaction.CommitAsync(ct);
    }

    private static async Task MergeMetadata(MangaContext mangaContext, Guid targetMangaId, Guid sourceMangaId,
        bool keepSourceMetadata, CancellationToken ct)
    {
        List<DbMangaMetadataEntries> targetEntries = await mangaContext.MangaMetadataEntries
            .Where(e => e.MangaId == targetMangaId).AsNoTracking().ToListAsync(ct);
        List<DbMangaMetadataEntries> sourceEntries = await mangaContext.MangaMetadataEntries
            .Where(e => e.MangaId == sourceMangaId).AsNoTracking().ToListAsync(ct);

        Guid? targetChosenId = targetEntries.FirstOrDefault(e => e.Chosen)?.MetadataId;
        Guid? sourceChosenId = sourceEntries.FirstOrDefault(e => e.Chosen)?.MetadataId;
        Guid? chosenMetadataId = keepSourceMetadata ? sourceChosenId ?? targetChosenId : targetChosenId ?? sourceChosenId;

        // A candidate Metadata-Entry can already be linked to both Manga (e.g. both were matched against the
        // same extension identifier) - drop the source side of any such duplicate before re-pointing, since the
        // composite (MangaId, MetadataId) primary key would otherwise collide once both point at the target.
        HashSet<Guid> targetMetadataIds = targetEntries.Select(e => e.MetadataId).ToHashSet();
        List<Guid> duplicateMetadataIds = sourceEntries.Select(e => e.MetadataId).Where(targetMetadataIds.Contains).ToList();

        if (duplicateMetadataIds.Count > 0)
            await mangaContext.MangaMetadataEntries
                .Where(e => e.MangaId == sourceMangaId && duplicateMetadataIds.Contains(e.MetadataId))
                .ExecuteDeleteAsync(ct);

        await mangaContext.MangaMetadataEntries.Where(e => e.MangaId == sourceMangaId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.MangaId, targetMangaId), ct);

        await mangaContext.MangaMetadataEntries.Where(e => e.MangaId == targetMangaId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Chosen, false), ct);

        if (chosenMetadataId is { } chosenId)
            await mangaContext.MangaMetadataEntries.Where(e => e.MangaId == targetMangaId && e.MetadataId == chosenId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Chosen, true), ct);
    }

    private static async Task MergeDownloadLinks(MangaContext mangaContext, Guid targetMangaId, Guid sourceMangaId, CancellationToken ct)
    {
        List<DbMangaDownloadLinks> targetLinks = await mangaContext.MangaDownloadLinks
            .Where(l => l.MangaId == targetMangaId).AsNoTracking().ToListAsync(ct);
        List<DbMangaDownloadLinks> sourceLinks = await mangaContext.MangaDownloadLinks
            .Where(l => l.MangaId == sourceMangaId).AsNoTracking().ToListAsync(ct);

        Dictionary<Guid, DbMangaDownloadLinks> targetByLinkId = targetLinks.ToDictionary(l => l.DownloadLinkId);

        // Both Manga can be linked to the same candidate Download-Link (composite PK MangaId+DownloadLinkId) - for
        // each such duplicate, keep whichever row is preferred (Matched over unmatched, then lower/better Priority)
        // and delete the other, so re-pointing the source's rows below never collides with the target's.
        List<Guid> deleteFromTarget = [];
        List<Guid> deleteFromSource = [];
        foreach (DbMangaDownloadLinks sourceLink in sourceLinks)
        {
            if (!targetByLinkId.TryGetValue(sourceLink.DownloadLinkId, out DbMangaDownloadLinks? targetLink))
                continue;

            bool targetWins = targetLink.Matched != sourceLink.Matched
                ? targetLink.Matched
                : targetLink.Priority <= sourceLink.Priority;

            if (targetWins)
                deleteFromSource.Add(sourceLink.DownloadLinkId);
            else
                deleteFromTarget.Add(sourceLink.DownloadLinkId);
        }

        if (deleteFromTarget.Count > 0)
            await mangaContext.MangaDownloadLinks
                .Where(l => l.MangaId == targetMangaId && deleteFromTarget.Contains(l.DownloadLinkId))
                .ExecuteDeleteAsync(ct);

        if (deleteFromSource.Count > 0)
            await mangaContext.MangaDownloadLinks
                .Where(l => l.MangaId == sourceMangaId && deleteFromSource.Contains(l.DownloadLinkId))
                .ExecuteDeleteAsync(ct);

        await mangaContext.MangaDownloadLinks.Where(l => l.MangaId == sourceMangaId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.MangaId, targetMangaId), ct);

        // Renormalize Priority (0..n-1) for the Matched links now on the target, preserving relative order. There
        // is no bulk row-numbering support in ExecuteUpdateAsync, so this is a small per-row loop, same as the
        // priority-shift logic in PatchMangaDownloadLinkEndpoint.
        List<DbMangaDownloadLinks> matchedLinks = await mangaContext.MangaDownloadLinks
            .Where(l => l.MangaId == targetMangaId && l.Matched)
            .OrderBy(l => l.Priority)
            .ToListAsync(ct);

        for (int i = 0; i < matchedLinks.Count; i++)
            matchedLinks[i].Priority = i;

        if (matchedLinks.Count > 0)
            await mangaContext.SaveChangesAsync(ct);
    }

    private static async Task MergeChapters(MangaContext mangaContext, Guid targetMangaId, Guid sourceMangaId,
        bool keepSourceChapters, CancellationToken ct)
    {
        if (!keepSourceChapters)
            return; // Source's Chapters are cascade-deleted along with the source Manga.

        List<Guid> targetChapterIds = await mangaContext.Chapters
            .Where(c => c.MangaId == targetMangaId).Select(c => c.ChapterId).ToListAsync(ct);

        if (targetChapterIds.Count > 0)
        {
            await mangaContext.ChapterDownloadLinks
                .Where(l => targetChapterIds.Contains(l.ChapterId))
                .ExecuteDeleteAsync(ct);

            await mangaContext.Chapters
                .Where(c => c.MangaId == targetMangaId)
                .ExecuteDeleteAsync(ct);
        }

        await mangaContext.Chapters.Where(c => c.MangaId == sourceMangaId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.MangaId, targetMangaId), ct);
    }
}

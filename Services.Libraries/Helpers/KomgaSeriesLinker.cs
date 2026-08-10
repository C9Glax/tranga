using Common.Helpers;
using Extensions.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.Libraries.Database;
using Services.Manga.Database;

namespace Services.Libraries.Helpers;

/// <summary>
/// Links Tranga manga to Komga series on a name-equality basis (the Komga series name matches the
/// manga's on-disk directory name), pushing metadata for each newly created link. Only considers
/// series in the Komga library Tranga owns (<see cref="DbLibraryService.TrangaLibraryId"/>), and
/// prunes mappings whose Komga series no longer exists there. Used when a Komga library is first
/// connected, when a user manually re-runs linking from the library settings page, and automatically
/// from <see cref="EventHandlers.MangaUpdatedHandler"/> (manga added/merged/synced) and
/// <see cref="EventHandlers.ChapterDownloadedHandler"/> (after triggering a scan) — in all cases
/// already-linked manga are skipped rather than re-added.
/// </summary>
internal static class KomgaSeriesLinker
{
    public static async Task<int> LinkExistingMangaByName(LibrariesContext ctx, MangaContext mangaContext, DbLibraryService dbLibraryService,
        Extensions.Extensions.Komga extension, ILogger logger, CancellationToken ct)
    {
        KomgaSeries[] seriesList = await extension.GetSeriesList(dbLibraryService.TrangaLibraryId, ct);
        HashSet<string> currentSeriesIds = seriesList.Select(s => (string)s.Id).ToHashSet();

        List<DbMangaMetadataEntries> mangaEntries = await mangaContext.MangaMetadataEntries
            .Where(e => e.Chosen == true)
            .ToListAsync(ct);

        List<DbMangaIdMapping> existingMappings = await ctx.MangaMappings
            .Where(m => m.LibraryServiceId == dbLibraryService.LibraryServiceId)
            .ToListAsync(ct);

        foreach (DbMangaIdMapping staleMapping in existingMappings.Where(m => !currentSeriesIds.Contains(m.SeriesId)))
            ctx.MangaMappings.Remove(staleMapping);

        HashSet<Guid> alreadyLinkedMangaIds = existingMappings
            .Where(m => currentSeriesIds.Contains(m.SeriesId))
            .Select(m => m.MangaId)
            .ToHashSet();

        int linkedCount = 0;
        foreach (DbMangaMetadataEntries entry in mangaEntries)
        {
            if (alreadyLinkedMangaIds.Contains(entry.MangaId))
                continue;

            string expectedName = entry.Metadata.Series.SafeFilesystemString();
            KomgaSeries? match = seriesList.FirstOrDefault(s => s.Name == expectedName);
            if (match is null)
                continue;

            try
            {
                await ctx.MangaMappings.AddAsync(new DbMangaIdMapping(dbLibraryService.LibraryServiceId, entry.MangaId, match.Id), ct);
                await KomgaMetadataSync.PushMetadata(mangaContext, extension, match.Id, entry.MangaId, ct);
                linkedCount++;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to link/push metadata for manga {MangaId} to Komga series {SeriesId}",
                    entry.MangaId, match.Id);
            }
        }

        return linkedCount;
    }
}

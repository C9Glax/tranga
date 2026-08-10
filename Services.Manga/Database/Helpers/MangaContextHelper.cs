using Microsoft.EntityFrameworkCore;

namespace Services.Manga.Database.Helpers;

/// <summary>
/// Query helpers for <see cref="MangaContext"/>.
/// </summary>
public static class MangaContextHelper
{
    /// <summary>
    /// Retrieves the currently chosen metadata entry (<c>Chosen == true</c>) for the given manga, if one exists.
    /// </summary>
    /// <param name="mangaContext">The database context to query.</param>
    /// <param name="mangaId">The identifier of the manga whose chosen metadata entry to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The chosen metadata entry, or <see langword="null"/> if none is set.</returns>
    public static async Task<DbMangaMetadataEntries?> GetManga(this global::Services.Manga.Database.MangaContext mangaContext, Guid mangaId, CancellationToken ct) =>
        await mangaContext.MangaMetadataEntries
            .Where(m => m.MangaId == mangaId)
            .FirstOrDefaultAsync(s => s.Chosen == true, ct);
}

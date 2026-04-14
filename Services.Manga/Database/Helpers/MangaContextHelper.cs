using Microsoft.EntityFrameworkCore;

namespace Services.Manga.Database.Helpers;

public static class MangaContextHelper
{
    public static async Task<DbMangaMetadataEntries?> GetManga(this global::Services.Manga.Database.MangaContext mangaContext, Guid mangaId, CancellationToken ct) =>
        await mangaContext.MangaMetadataEntries
            .Where(m => m.MangaId == mangaId)
            .FirstOrDefaultAsync(s => s.Chosen == true, ct);
}
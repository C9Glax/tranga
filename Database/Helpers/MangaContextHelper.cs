using Database.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace Database.Helpers;

public static class MangaContextHelper
{
    public static async Task<DbMangaMetadataEntries?> GetManga(this MangaContext.MangaContext mangaContext, Guid mangaId, CancellationToken ct) =>
        await mangaContext.MangaMetadataEntries
            .Where(m => m.MangaId == mangaId)
            .FirstOrDefaultAsync(s => s.Chosen == true, ct);
}
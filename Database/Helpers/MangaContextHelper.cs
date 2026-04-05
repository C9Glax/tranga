using Database.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace Database.Helpers;

public static class MangaContextHelper
{
    public static async Task<DbMangaMetadataSource?> GetManga(this MangaContext.MangaContext mangaContext, Guid mangaId, CancellationToken ct) =>
        await mangaContext.MangaMetadataSources
            .Where(m => m.MangaId == mangaId)
            .FirstOrDefaultAsync(s => s.Chosen == true, ct);
}
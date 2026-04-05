using Database.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace Database.Helpers;

public static class MangaContextHelper
{
    public static async Task<DbMetadataSource?> GetManga(this MangaContext.MangaContext mangaContext, Guid mangaId,
        CancellationToken ct) => await mangaContext.MetadataSources.Include(s => s.Manga).Where(m => m.MangaId == mangaId).OrderBy(s => s.Priority)
        .FirstOrDefaultAsync(ct);
}
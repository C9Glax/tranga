using Database.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace Database.Helpers;

public static class DbMangaHelper
{
    public static IQueryable<DbManga> GetManga(this IQueryable<DbManga> query, Guid mangaId) =>
        query.Where(m => m.Id == mangaId);
    
    public static IQueryable<DbManga> IncludeDownloadLinks(this IQueryable<DbManga> query, bool include) =>
        include ? query.Include(m => m.DownloadLinks) : query;
    
    public static IQueryable<DbManga> IncludeMetadataLinks(this IQueryable<DbManga> query, bool include) =>
        include ? query.Include(m => m.MetadataLinks) : query;

    public static IQueryable<DbMetadataLink> GetMetadataLink(this IQueryable<DbManga> query, Guid metadataLink) =>
        query.Include(m => m.MetadataLinks).SelectMany(m => m.MetadataLinks!)
            .Where(l => l.MetadataExtensionId == metadataLink);
}
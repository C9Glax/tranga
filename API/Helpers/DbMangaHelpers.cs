using API.DTOs;
using Database.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Helpers;

internal static class DbMangaHelpers
{
    public static MangaDTO ToDTO(this DbManga manga) => new()
    {
        MangaId = manga.Id,
        Title = manga.Series,
        Monitored = manga.Monitor,
        DownloadLinks = manga.DownloadLinks?.Select(d => d.ToDTO()).ToArray(),
        MetadataLinks = manga.MetadataLinks?.Select(l => l.ToDTO()).ToArray(),
    };

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
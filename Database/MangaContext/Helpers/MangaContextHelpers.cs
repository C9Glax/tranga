using Common.Helpers;
using MetadataExtensions.Extensions;
using Microsoft.EntityFrameworkCore;
using ComicInfo = MetadataExtensions.ComicInfo;

namespace Database.MangaContext.Helpers;

public static class MangaContextHelpers
{
    public static async Task InsertNewDataIntoContext(this MangaContext ctx, List<ComicInfo> comicInfos, CancellationToken ct)
    {
        foreach (ComicInfo comicInfo in comicInfos)
        {
            if (await ctx.Mangas.Include(m => m.ComicInfo).FirstOrDefaultAsync(m => m.ComicInfo!.Title == comicInfo.Title, ct) is not { } existing)
            {
                DbManga manga = CreateManga(comicInfo);
                await ctx.Mangas.AddAsync(manga, ct);
            }
            else
            {
                ComicInfo mergedComicInfo = existing.ComicInfo!.Merge(comicInfo);
                existing.ComicInfo = mergedComicInfo;
            }
        }
    }

    internal static DbManga CreateManga(ComicInfo comicInfo) => new()
    {
        MangaUpdatesSeriesId = comicInfo is MangaUpdateComicInfo ci
            ? ci.MangaUpdatesSeriesId
            : null,
        ComicInfo = comicInfo,
        CoverImageBase64 = comicInfo.Cover.ToCoverBase64()
    };

    internal static ComicInfo Merge(this Common.Datatypes.ComicInfo comicInfo, ComicInfo other) => other with
    {
        Summary = string.IsNullOrEmpty(comicInfo.Summary) ? other.Summary : comicInfo.Summary
        // TODO
    };
    
    public static IQueryable<DbManga> FilterManga(this IQueryable<DbManga> queryable, Guid mangaId) =>
        queryable.Where(m => m.MangaId == mangaId);

    public static IQueryable<DbManga> FilterManga(this MangaContext set, Guid mangaId) => set.Mangas.FilterManga(mangaId);
}
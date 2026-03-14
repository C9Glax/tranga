using Common.Datatypes.Helpers;
using Data;
using MetadataExtensions.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Database.MangaContext.Helpers;

public static class MangaContextHelpers
{
    public static async Task<List<ComicInfo>> MergeComicInfos(this MangaContext ctx, List<ComicInfo> comicInfos, CancellationToken ct)
    {
        List<ComicInfo> ret = new();
        foreach (ComicInfo comicInfo in comicInfos)
        {
            if (await ctx.Mangas.Include(m => m.ComicInfo).FirstOrDefaultAsync(m => m.ComicInfo!.Title == comicInfo.Title, ct) is not { } existing)
            {
                DbManga manga = CreateManga(comicInfo);
                await ctx.Mangas.AddAsync(manga, ct);
                ret.Add(comicInfo);
            }
            else
            {
                ComicInfo mergedComicInfo = existing.ComicInfo!.Merge(comicInfo);
                existing.ComicInfo = mergedComicInfo;
                ret.Add(mergedComicInfo);
            }
        }

        return ret;
    }

    internal static DbManga CreateManga(ComicInfo comicInfo) => new ()
    {
        MangaUpdatesSeriesId = comicInfo is MangaUpdateComicInfo ci ? ci.MangaUpdatesSeriesId : null,
        ComicInfo = comicInfo
    };
}
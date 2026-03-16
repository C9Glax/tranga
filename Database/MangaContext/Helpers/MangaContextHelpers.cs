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
            if (await ctx.Mangas.FirstOrDefaultAsync(m => m.Title == comicInfo.Title, ct) is not { } existing)
            {
                DbManga manga = CreateManga(comicInfo);
                await ctx.Mangas.AddAsync(manga, ct);
            }
            else
            {
                existing.Merge(comicInfo);
            }
        }
    }

    private static DbManga CreateManga(ComicInfo comicInfo) => new()
    {
        MangaUpdatesSeriesId = comicInfo is MangaUpdateComicInfo ci
            ? ci.MangaUpdatesSeriesId
            : null,
        CoverImageBase64 = comicInfo.Cover.ToCoverBase64(),
        Title = !string.IsNullOrEmpty(comicInfo.Series) ? comicInfo.Series : comicInfo.Title,
        Description = !string.IsNullOrEmpty(comicInfo.Summary) ? comicInfo.Summary : null,
        Year = comicInfo.Year != default ? comicInfo.Year : null,
        Authors = !string.IsNullOrEmpty(comicInfo.Writer) ? comicInfo.Writer.Split(',') : null,
        Artists = !string.IsNullOrEmpty(comicInfo.Penciller) ? comicInfo.Penciller.Split(',') : null,
        Genre = !string.IsNullOrEmpty(comicInfo.Genre) ? comicInfo.Genre.Split(',') : null,
        Tags = !string.IsNullOrEmpty(comicInfo.Notes) ? comicInfo.Notes.Split('.') : null,
        AgeRating = comicInfo.AgeRating != default ? comicInfo.AgeRating : null
    };

    internal static void Merge(this DbManga manga, ComicInfo other)
    {
        // TODO
    }
    
    public static IQueryable<DbManga> FilterManga(this IQueryable<DbManga> queryable, Guid mangaId) =>
        queryable.Where(m => m.MangaId == mangaId);

    public static IQueryable<DbManga> FilterManga(this MangaContext set, Guid mangaId) => set.Mangas.FilterManga(mangaId);
}
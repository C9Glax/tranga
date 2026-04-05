using API.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

public abstract class GetMangaListEndpoint
{
    public static async Task<Results<Ok<Entities.MangaMetadata[]>, InternalServerError>> Handle(MangaContext mangaContext, [FromQuery] bool? includeUnmonitored, CancellationToken ct)
    {
        IQueryable<DbManga> query = includeUnmonitored switch
        {
            true => mangaContext.Mangas,
            _ => mangaContext.Mangas.Where(m => m.Monitored == true)
        };
        if(await query.Include(m => m.MetadataSources).Select(m => m.MetadataSources!.OrderBy(s => s.Priority).FirstOrDefault()).ToListAsync(ct) is not { } metadataSources)
            return TypedResults.InternalServerError();

        metadataSources = metadataSources.Where(m => m is not null).ToList();

        Entities.MangaMetadata[] result = metadataSources.Select(m => m!.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}
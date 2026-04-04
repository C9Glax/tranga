using API.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

public abstract class GetMangaListEndpoint
{
    public static async Task<Results<Ok<Entities.MangaMetadata[]>, InternalServerError>> Handle(MangaContext mangaContext, CancellationToken ct)
    {
        if (await mangaContext.MetadataSources.OrderBy(s => s.Priority).GroupBy(s => s.MangaId).Select(g => g.First()).ToListAsync(ct) is not { } metadataSources)
            return TypedResults.InternalServerError();

        Entities.MangaMetadata[] result = metadataSources.Select(m => m.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}
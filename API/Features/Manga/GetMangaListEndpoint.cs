using API.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

public abstract class GetMangaListEndpoint
{
    public static async Task<Results<Ok<Entities.Manga[]>, InternalServerError>> Handle(MangaContext mangaContext, CancellationToken ct)
    {
        if (await mangaContext.MangaMetadataSources
                .Where(s => s.Chosen == true)
                .ToListAsync(ct) is not { } metadataSources)
        {
            return TypedResults.InternalServerError();
        }

        Entities.Manga[] result = metadataSources.Select(m => m.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}
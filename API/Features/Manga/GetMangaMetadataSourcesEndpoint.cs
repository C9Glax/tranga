using API.Entities;
using API.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

public abstract class GetMangaMetadataSourcesEndpoint
{
    public static async Task<Results<Ok<Entities.Metadata[]>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.MangaMetadataSources.Where(s => s.MangaId == mangaId).ToListAsync(ct) is not { } sources)
            return TypedResults.NotFound();

        Entities.Metadata[] result = sources.Select(s => s.ToDTO()).Select(d => d.MetadataEntry!).ToArray();
        return TypedResults.Ok(result);
    }
}
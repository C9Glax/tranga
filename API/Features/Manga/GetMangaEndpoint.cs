using API.Helpers;
using Database.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace API.Features.Manga;

public class GetMangaEndpoint
{
    public static async Task<Results<Ok<Entities.Manga>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.GetManga(mangaId, ct) is not { } manga)
            return TypedResults.NotFound();

        return TypedResults.Ok(manga.ToDTO());
    }
}
using API.Entities;
using API.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

public abstract class GetMangaDownloadSourcesEndpoint
{
    public static async Task<Results<Ok<DownloadLink[]>, NotFound>> Handle(MangaContext mangaContext, [FromRoute] Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.MangaDownloadSources.Where(e => e.MangaId == mangaId && e.Matched == true).ToListAsync(ct) is not { } list)
            return TypedResults.NotFound();

        DownloadLink[] results = list.Select(e => e.ToDTO()).ToArray();
        return TypedResults.Ok(results);
    }
}
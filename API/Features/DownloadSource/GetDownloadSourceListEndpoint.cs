using API.Entities;
using API.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace API.Features.DownloadSource;

public abstract class GetDownloadSourceListEndpoint
{
    public static async Task<Results<Ok<DownloadLink[]>, InternalServerError>> Handle(MangaContext mangaContext,  CancellationToken ct)
    {
        if (await mangaContext.MangaDownloadSources.ToListAsync(ct) is not { } list)
            return TypedResults.InternalServerError();

        DownloadLink[] result = list.Select(e => e.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}
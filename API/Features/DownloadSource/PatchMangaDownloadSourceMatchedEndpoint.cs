using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.DownloadSource;

public abstract class PatchMangaDownloadSourceMatchedEndpoint
{
    public static async Task<Results<Ok, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, [FromRoute]Guid downloadId, [FromBody]PatchMangaDownloadSourceMatchedRequest req, CancellationToken ct)
    {
        if (await mangaContext.MangaDownloadSources.FirstOrDefaultAsync(s => s.DownloadSourceId == downloadId && s.MangaId == mangaId, ct) is not { } entry)
            return TypedResults.NotFound();
        
        //TODO Priority
        
        entry.Matched = req.Matched;
        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    public sealed record PatchMangaDownloadSourceMatchedRequest(bool Matched, int Priority);
}
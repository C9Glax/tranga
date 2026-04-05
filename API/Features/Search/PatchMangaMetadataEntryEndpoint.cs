using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Search;

public abstract class PatchMangaMetadataEntryEndpoint
{
    public static async Task<Results<Ok, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, [FromBody]PatchMangaMetadataEntryRequest req, CancellationToken ct)
    {
        if (await mangaContext.MangaMetadataSources.FirstOrDefaultAsync(
                s => s.MangaId == mangaId && s.MetadataSourceId == req.metadataId, ct) is not { } entry)
            return TypedResults.NotFound();

        await mangaContext.MangaMetadataSources.ExecuteUpdateAsync(s => s.SetProperty(p => p.Chosen, false), cancellationToken: ct);

        entry.Chosen = true;
        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    public sealed record PatchMangaMetadataEntryRequest(Guid metadataId);
}
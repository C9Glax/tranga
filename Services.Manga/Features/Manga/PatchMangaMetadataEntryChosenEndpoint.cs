using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Manga.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PatchMangaMetadataEntryChosenEndpoint
{
    /// <summary>
    /// Sets a Metadata-Entry as chosen "Source of Truth" for Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of Manga</param>
    /// <param name="metadataId">ID of Metadata-Entry</param>
    /// <param name="ct"></param>
    /// <response code="200">Metadata-Entry has been chosen</response>
    /// <response code="404">Manga or Metadata with requested ID does not exist</response>
    public static async Task<Results<Ok, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, [FromRoute]Guid metadataId, CancellationToken ct)
    {
        if (await mangaContext.MangaMetadataEntries.FirstOrDefaultAsync(
                s => s.MangaId == mangaId && s.MetadataId == metadataId, ct) is not { } entry)
            return TypedResults.NotFound();

        await mangaContext.MangaMetadataEntries.Where(e => e.MangaId == mangaId).ExecuteUpdateAsync(s => s.SetProperty(p => p.Chosen, false), cancellationToken: ct);

        entry.Chosen = true;
        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
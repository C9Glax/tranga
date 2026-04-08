using Services.Manga.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Services.Manga.Features.Metadata;

/// <summary>
/// <inheritdoc cref="Handle" path="summary/" />
/// </summary>
public abstract class GetMetadataMangaEndpoint
{
    /// <summary>
    /// Manga the Metadata-Entry is linked to (by Search)
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="metadataId">ID of the Metadata-Entry</param>
    /// <param name="ct"></param>
    /// <returns>Manga</returns>
    /// <response code="200">Manga</response>
    /// <response code="404">Metadata-Entry with ID not found</response>
    public static async Task<Results<Ok<Entities.Manga>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid metadataId, CancellationToken ct)
    {
        if (await mangaContext.MangaMetadataEntries.FirstOrDefaultAsync(s => s.MetadataId == metadataId && s.Chosen, ct) is not { } manga)
            return TypedResults.NotFound();

        return TypedResults.Ok(manga.ToDTO());
    }
}
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Manga.Features.Metadata;

/// <summary>
/// <inheritdoc cref="Handle" />
/// </summary>
public abstract class GetMetadataRelatedMangaIdsEndpoint
{
    /// <summary>
    /// IDs of Manga the Metadata-Entry is related to
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="metadataId">ID of the Metadata-Entry</param>
    /// <param name="ct"></param>
    /// <returns>Manga-IDs</returns>
    /// <response code="200">Manga-IDs</response>
    /// <response code="404">Metadata-Entry with ID not found</response>
    public static async Task<Results<Ok<Guid[]>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid metadataId, CancellationToken ct)
    {
        if (await mangaContext.MangaMetadataEntries.Where(s => s.MetadataId == metadataId).Select(s => s.MangaId).ToListAsync(ct) is not { } ids)
            return TypedResults.NotFound();

        return TypedResults.Ok(ids.ToArray());
    }
}
using Services.Manga.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Services.Manga.Features.Metadata;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetMetadataEntryEndpoint
{
    /// <summary>
    /// Get Metadata-Entry
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="metadataId">ID of Metadata-Entry</param>
    /// <param name="ct"></param>
    /// <returns>The Metadata-Entry</returns>
    /// <response code="200">The Metadata-Entry</response>
    public static async Task<Results<Ok<Entities.Metadata>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid metadataId, CancellationToken ct)
    {
        if (await mangaContext.MetadataEntries.FirstOrDefaultAsync(e => e.MetadataId == metadataId, ct) is not { } metadata)
            return TypedResults.NotFound();
        
        return TypedResults.Ok(metadata.ToDTO());
    }
}
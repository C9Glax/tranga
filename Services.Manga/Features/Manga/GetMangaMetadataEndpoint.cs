using Services.Manga.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Services.Manga.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetMangaMetadataEndpoint
{
    /// <summary>
    /// Metadata of Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of Manga</param>
    /// <param name="ct"></param>
    /// <returns>Metadata of Manga</returns>
    /// <response code="200">Metadata of Manga</response>
    /// <response code="404">Manga with requested ID does not exist or no Metadata-Entry has been chosen for Manga</response>
    public static async Task<Results<Ok<Entities.Metadata>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.MangaMetadataEntries.Where(e => e.MangaId == mangaId && e.Chosen).Select(e => e.Metadata)
                .FirstOrDefaultAsync(ct) is not { } metadata)
            return TypedResults.NotFound();

        return TypedResults.Ok(metadata.ToDTO());
    }
}
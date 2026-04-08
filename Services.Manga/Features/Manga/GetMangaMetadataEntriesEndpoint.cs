using Services.Manga.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Services.Manga.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetMangaMetadataEntriesEndpoint
{
    /// <summary>
    /// Metadata-Entries related to Manga (by Search)
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of Manga</param>
    /// <param name="ct"></param>
    /// <returns>Metadata-Entries of Manga</returns>
    /// <response code="200">Metadata-Entries of Manga</response>
    /// <response code="404">Manga with requested ID does not exist</response>
    public static async Task<Results<Ok<Entities.Metadata[]>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.MangaMetadataEntries.Where(e => e.MangaId == mangaId).Select(e => e.Metadata)
                .ToListAsync(ct) is not { } list)
            return TypedResults.NotFound();

        Entities.Metadata[] result = list.Select(l => l.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}
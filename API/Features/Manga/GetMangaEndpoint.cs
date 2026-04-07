using API.Helpers;
using Database.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace API.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetMangaEndpoint
{
    /// <summary>
    /// Manga with ID
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of Manga</param>
    /// <param name="ct"></param>
    /// <returns>Manga with ID and Metadata</returns>
    /// <response code="200">Manga with ID and Metadata</response>
    /// <response code="404">Manga with requested ID does not exist or no Metadata-Entry has been chosen for Manga</response>
    public static async Task<Results<Ok<Entities.Manga>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.GetManga(mangaId, ct) is not { } manga)
            return TypedResults.NotFound();

        return TypedResults.Ok(manga.ToDTO());
    }
}
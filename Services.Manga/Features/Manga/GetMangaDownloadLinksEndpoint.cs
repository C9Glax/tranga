using Services.Manga.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Entities;

namespace Services.Manga.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetMangaDownloadLinksEndpoint
{
    /// <summary>
    /// Download-Links of Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of Manga</param>
    /// <param name="ct"></param>
    /// <returns>List of Download-Links used for Manga</returns>
    /// <response code="200">List of Download-Links used for Manga</response>
    /// <response code="404">Manga with requested ID does not exist</response>
    public static async Task<Results<Ok<MangaDownloadLink[]>, NotFound>> Handle(MangaContext mangaContext, [FromRoute] Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.MangaDownloadLinks.Where(e => e.MangaId == mangaId && e.Matched == true).ToListAsync(ct) is not { } list)
            return TypedResults.NotFound();

        MangaDownloadLink[] results = list.Select(e => e.ToDTO()).ToArray();
        return TypedResults.Ok(results);
    }
}
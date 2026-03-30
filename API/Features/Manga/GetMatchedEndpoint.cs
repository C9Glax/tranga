using API.DTOs;
using API.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

/// <summary>
/// Returns the DownloadLinks of Manga with the requested ID
/// </summary>
public abstract class GetMatchedEndpoint
{
    /// <summary>
    /// Returns the DownloadLinks of Manga with the requested ID
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of the Manga</param>
    /// <param name="ct"></param>
    /// <returns>A List of DownloadLinks</returns>
    /// <response code="200">A List of DownloadLinks</response>
    /// <response code="404">Manga could not be found</response>
    public static async Task<Results<Ok<DownloadLinkDTO[]>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.DownloadLinks.Where(d => d.MangaId == mangaId).ToListAsync(ct) is not { } links)
            return TypedResults.NotFound();

        return TypedResults.Ok(links.Select(l => l.ToDTO()).ToArray());
    }
}
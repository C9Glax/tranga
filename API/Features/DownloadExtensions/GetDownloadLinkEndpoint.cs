using API.DTOs;
using API.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.DownloadExtensions;

/// <summary>
/// Returns the DownloadLink with the requested ID
/// </summary>
public abstract class GetDownloadLinkEndpoint
{
    /// <summary>
    /// Returns the DownloadLink with the requested ID
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="matchId">ID of the DownloadLink</param>
    /// <param name="ct"></param>
    /// <response code="200">DownloadLink with ID</response>
    /// <response code="404">DownloadLink could not be found</response>
    public static async Task<Results<Ok<DownloadLinkDTO>, NotFound>> Handle(MangaContext mangaContext, [FromRoute] Guid matchId, CancellationToken ct)
    {
        if (await mangaContext.DownloadLinks.FirstOrDefaultAsync(l => l.Id == matchId, ct) is not { } link)
            return TypedResults.NotFound();

        return TypedResults.Ok(link.ToDTO());
    }
}
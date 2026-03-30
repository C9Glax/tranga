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
    /// <param name="downloadLinkId">ID of the DownloadLink</param>
    /// <param name="ct"></param>
    public static async Task<Results<Ok<DownloadLinkDTO>, NotFound>> Handle(MangaContext mangaContext, [FromRoute] Guid downloadLinkId, CancellationToken ct)
    {
        if (await mangaContext.DownloadLinks.FirstOrDefaultAsync(l => l.Id == downloadLinkId, ct) is not { } link)
            return TypedResults.NotFound();

        return TypedResults.Ok(link.ToDTO());
    }
}
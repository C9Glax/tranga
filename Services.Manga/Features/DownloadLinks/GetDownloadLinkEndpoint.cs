using Services.Manga.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Entities;

namespace Services.Manga.Features.DownloadLinks;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetDownloadLinkEndpoint
{
    /// <summary>
    /// Download-Link with ID
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="downloadId">ID of Download-Link</param>
    /// <param name="ct"></param>
    /// <returns>Download-Link</returns>
    /// <response code="200">Download-Link</response>
    /// <response code="404">Download-Link with ID does not exist</response>
    public static async Task<Results<Ok<DownloadLink>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid downloadId,  CancellationToken ct)
    {
        if (await mangaContext.DownloadLinks.FirstOrDefaultAsync(d => d.DownloadLinkId == downloadId,ct) is not { } entry)
            return TypedResults.NotFound();

        return TypedResults.Ok(entry.ToDTO());
    }
}
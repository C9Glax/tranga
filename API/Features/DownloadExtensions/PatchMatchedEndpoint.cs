using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.DownloadExtensions;

/// <summary>
/// Marks a DownloadLink as Resource for a Manga
/// </summary>
public abstract class PatchMatchedEndpoint
{
    /// <summary>
    /// Marks a DownloadLink as Resource for a Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="downloadLinkId">ID of the DownloadLink</param>
    /// <param name="matched"></param>
    /// <param name="ct"></param>
    public static async Task<Results<Ok, NotFound>> Handle(MangaContext mangaContext, [FromRoute] Guid downloadLinkId, [FromQuery]bool matched, CancellationToken ct)
    {
        if (await mangaContext.DownloadLinks.FirstOrDefaultAsync(l => l.Id == downloadLinkId, ct) is not { } link)
            return TypedResults.NotFound();

        link.Matched = matched;

        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
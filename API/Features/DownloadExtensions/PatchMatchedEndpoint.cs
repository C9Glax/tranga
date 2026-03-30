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
    /// <param name="matchId">ID of the DownloadLink</param>
    /// <param name="matched"></param>
    /// <param name="ct"></param>
    /// <response code="200">Matched changed</response>
    /// <response code="404">DownloadLink could not be found</response>
    public static async Task<Results<Ok, NotFound>> Handle(MangaContext mangaContext, [FromRoute] Guid matchId, [FromQuery]bool matched, CancellationToken ct)
    {
        if (await mangaContext.DownloadLinks.FirstOrDefaultAsync(l => l.Id == matchId, ct) is not { } link)
            return TypedResults.NotFound();

        link.Matched = matched;

        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
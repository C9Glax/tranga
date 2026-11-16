using Common.Services.Events;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Manga.Features.CustomDownloaders;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class DeleteCustomDownloaderEndpoint
{
    /// <summary>
    /// Delete a custom (selector based) Download-Source.
    /// Download-Links and chapters that were created through it are kept, but cannot fetch or download anything anymore.
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="eventPublisher"></param>
    /// <param name="customDownloaderId">ID of the Download-Source</param>
    /// <param name="ct"></param>
    /// <response code="200">The Download-Source was deleted</response>
    /// <response code="404">No custom Download-Source with that ID exists</response>
    public static async Task<Results<Ok, NotFound>> Handle(
        [FromServices] MangaContext mangaContext, [FromServices] EventPublisher eventPublisher,
        [FromRoute] Guid customDownloaderId, CancellationToken ct)
    {
        if (await mangaContext.CustomDownloaders.FirstOrDefaultAsync(c => c.CustomDownloaderId == customDownloaderId, ct) is not { } row)
            return TypedResults.NotFound();

        mangaContext.CustomDownloaders.Remove(row);
        await mangaContext.SaveChangesAsync(ct);

        await CustomDownloaderChanges.ApplyAsync(eventPublisher, ct);
        return TypedResults.Ok();
    }
}

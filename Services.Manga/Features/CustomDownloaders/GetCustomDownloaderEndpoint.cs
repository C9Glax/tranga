using Extensions.Custom;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Helpers;

namespace Services.Manga.Features.CustomDownloaders;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetCustomDownloaderEndpoint
{
    /// <summary>
    /// Get a custom (selector based) Download-Source
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="customDownloaderId">ID of the Download-Source</param>
    /// <param name="ct"></param>
    /// <response code="200">The definition of the Download-Source</response>
    /// <response code="404">No custom Download-Source with that ID exists</response>
    public static async Task<Results<Ok<SelectorDownloaderDefinition>, NotFound>> Handle(
        [FromServices] MangaContext mangaContext, [FromRoute] Guid customDownloaderId, CancellationToken ct)
    {
        if (await mangaContext.CustomDownloaders.AsNoTracking().FirstOrDefaultAsync(c => c.CustomDownloaderId == customDownloaderId, ct) is not { } row ||
            CustomDownloaderStore.ToDefinition(row) is not { } definition)
            return TypedResults.NotFound();

        return TypedResults.Ok(definition);
    }
}

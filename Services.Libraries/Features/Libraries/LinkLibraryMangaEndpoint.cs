using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.Libraries.Database;
using Services.Libraries.Helpers;
using Services.Manga.Database;

namespace Services.Libraries.Features.Libraries;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class LinkLibraryMangaEndpoint
{
    /// <summary>
    /// Manually links unmapped Tranga manga to Komga series on a name-equality basis, and pushes
    /// metadata for each newly created link. Re-runs the same linking Komga libraries get on connect,
    /// for manga added or downloaded after the library was already connected.
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="mangaContext"></param>
    /// <param name="libraryId">ID of the library extension</param>
    /// <param name="logger"></param>
    /// <param name="ct"></param>
    /// <returns>Number of manga newly linked</returns>
    /// <response code="200">Number of manga newly linked</response>
    /// <response code="404">Library with ID does not exist</response>
    /// <response code="400">Library type does not support linking</response>
    public static async Task<Results<Ok<int>, NotFound, BadRequest<string>>> Handle(LibrariesContext ctx, MangaContext mangaContext,
        [FromRoute]Guid libraryId, ILogger<LinkLibraryMangaEndpoint> logger, CancellationToken ct)
    {
        DbLibraryService? dbLibrary = await ctx.LibraryServices.SingleOrDefaultAsync(l => l.LibraryServiceId == libraryId, ct);
        if (dbLibrary is null)
            return TypedResults.NotFound();

        if (dbLibrary.ToExtension() is not { } extension)
            return TypedResults.BadRequest("Unsupported library type.");

        int linkedCount = await KomgaSeriesLinker.LinkExistingMangaByName(ctx, mangaContext, dbLibrary, extension, logger, ct);
        await ctx.SaveChangesAsync(ct);

        return TypedResults.Ok(linkedCount);
    }
}

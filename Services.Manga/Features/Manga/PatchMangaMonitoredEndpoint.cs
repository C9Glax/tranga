using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Manga.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PatchMangaMonitoredEndpoint
{
    /// <summary>
    /// Set the Monitored status of a Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of Manga</param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <response code="200">Monitored status has been changed</response>
    /// <response code="404">Manga with requested ID does not exist</response>
    public static async Task<Results<Ok, NotFound>> Handle([FromServices]MangaContext mangaContext, [FromRoute]Guid mangaId, [FromBody]PatchMangaMonitoredRequest req, CancellationToken ct)
    {
        if (await mangaContext.Mangas.FirstOrDefaultAsync(m => m.MangaId == mangaId, ct) is not { } manga)
            return TypedResults.NotFound();

        manga.Monitored = req.Monitored;

        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    /// <summary>
    /// Used in <see cref="PatchMangaMonitoredEndpoint"/>
    /// </summary>
    /// <param name="Monitored">Whether the Manga should be periodically checked for new Chapters and Metadata</param>
    public sealed record PatchMangaMonitoredRequest(bool Monitored);
}

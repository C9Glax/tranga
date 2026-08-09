using Common.Services.Events;
using Common.Services.Events.Events;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Manga.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PostMangaSyncEndpoint
{
    /// <summary>
    /// Manually triggers a metadata sync of a Manga's chosen metadata (and cover) to any linked
    /// library extensions (e.g. Komga). No-op if the Manga isn't linked to any library.
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="eventPublisher"></param>
    /// <param name="mangaId">ID of Manga</param>
    /// <param name="ct"></param>
    /// <response code="200">Sync has been queued</response>
    /// <response code="404">Manga with requested ID does not exist</response>
    public static async Task<Results<Ok, NotFound>> Handle(MangaContext mangaContext, [FromServices]EventPublisher eventPublisher, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        if (!await mangaContext.Mangas.AnyAsync(m => m.MangaId == mangaId, ct))
            return TypedResults.NotFound();

        await eventPublisher.PublishAsync(new MangaUpdatedEvent(mangaId), ct);

        return TypedResults.Ok();
    }
}

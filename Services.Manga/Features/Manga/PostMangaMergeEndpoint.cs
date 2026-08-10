using Common.Services.Events;
using Common.Services.Events.Events;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;

namespace Services.Manga.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PostMangaMergeEndpoint
{
    /// <summary>
    /// Merges another Manga into this one and deletes it. Metadata candidates and Download-Link matches from both
    /// Manga are combined onto this one; which side's chosen Metadata-Entry (Title/Summary/Cover) and Chapters
    /// survive is controlled by the request body and default to this (the target) Manga's own.
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="eventPublisher"></param>
    /// <param name="mangaId">ID of the Manga that survives the merge</param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <response code="200">Manga has been merged</response>
    /// <response code="400">Attempted to merge a Manga into itself</response>
    /// <response code="404">Manga or source Manga with requested ID does not exist</response>
    public static async Task<Results<Ok, NotFound, BadRequest<string>>> Handle(MangaContext mangaContext,
        [FromServices] EventPublisher eventPublisher, [FromRoute] Guid mangaId, [FromBody] PostMangaMergeRequest req, CancellationToken ct)
    {
        if (req.SourceMangaId == mangaId)
            return TypedResults.BadRequest("Cannot merge a Manga into itself.");

        if (!await mangaContext.Mangas.AnyAsync(m => m.MangaId == mangaId, ct)
            || !await mangaContext.Mangas.AnyAsync(m => m.MangaId == req.SourceMangaId, ct))
            return TypedResults.NotFound();

        await mangaContext.MergeInto(mangaId, req.SourceMangaId, req.KeepSourceMetadata, req.KeepSourceChapters, ct);

        await eventPublisher.PublishAsync(new MangaMergedEvent(req.SourceMangaId, mangaId), ct);
        await eventPublisher.PublishAsync(new MangaUpdatedEvent(mangaId), ct);

        return TypedResults.Ok();
    }

    /// <summary>
    /// Used in <see cref="PostMangaMergeEndpoint"/>
    /// </summary>
    /// <param name="SourceMangaId">ID of the Manga to merge into this one and delete</param>
    /// <param name="KeepSourceMetadata">Use the source Manga's chosen Metadata-Entry (Title/Summary/Cover) instead of the target's own</param>
    /// <param name="KeepSourceChapters">Use the source Manga's Chapters instead of the target's own</param>
    public sealed record PostMangaMergeRequest(Guid SourceMangaId, bool KeepSourceMetadata, bool KeepSourceChapters);
}

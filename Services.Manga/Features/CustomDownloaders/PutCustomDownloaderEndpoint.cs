using Common.Services.Events;
using Extensions.Custom;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Manga.Features.CustomDownloaders;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PutCustomDownloaderEndpoint
{
    /// <summary>
    /// Change a custom (selector based) Download-Source
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="eventPublisher"></param>
    /// <param name="customDownloaderId">ID of the Download-Source</param>
    /// <param name="definition">The new definition. Its ID is ignored, the ID of the route is kept</param>
    /// <param name="ct"></param>
    /// <response code="200">The Download-Source was changed</response>
    /// <response code="400">The definition is invalid; the body lists everything that is wrong with it</response>
    /// <response code="404">No custom Download-Source with that ID exists</response>
    public static async Task<Results<Ok<SelectorDownloaderDefinition>, BadRequest<string[]>, NotFound>> Handle(
        [FromServices] MangaContext mangaContext, [FromServices] EventPublisher eventPublisher,
        [FromRoute] Guid customDownloaderId, [FromBody] SelectorDownloaderDefinition definition, CancellationToken ct)
    {
        if (await mangaContext.CustomDownloaders.FirstOrDefaultAsync(c => c.CustomDownloaderId == customDownloaderId, ct) is not { } row)
            return TypedResults.NotFound();

        definition = (definition with { Id = customDownloaderId }).Normalized();
        if (definition.Validate() is { Count: > 0 } errors)
            return TypedResults.BadRequest(errors.ToArray());

        row.Name = definition.Name;
        row.DefinitionJson = SelectorDownloaderJson.Serialize(definition);
        row.UpdatedAt = DateTime.UtcNow;
        await mangaContext.SaveChangesAsync(ct);

        await CustomDownloaderChanges.ApplyAsync(eventPublisher, ct);
        return TypedResults.Ok(definition);
    }
}

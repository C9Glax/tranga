using Common.Services.Events;
using Extensions.Custom;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Manga.Database;

namespace Services.Manga.Features.CustomDownloaders;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PostCustomDownloaderEndpoint
{
    /// <summary>
    /// Create a custom (selector based) Download-Source
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="eventPublisher"></param>
    /// <param name="definition">The source. Its ID is ignored, a new one is assigned</param>
    /// <param name="ct"></param>
    /// <response code="200">The Download-Source was created and is available as Download-Extension straight away</response>
    /// <response code="400">The definition is invalid; the validation problem lists everything that is wrong with it</response>
    public static async Task<Ok<SelectorDownloaderDefinition>> Handle(
        [FromServices] MangaContext mangaContext, [FromServices] EventPublisher eventPublisher,
        [FromBody] SelectorDownloaderDefinition definition, CancellationToken ct)
    {
        // The definition validated itself before we got here (see SelectorDownloaderDefinition.Validate).
        definition = (definition with { Id = Guid.NewGuid() }).Normalized();

        await mangaContext.CustomDownloaders.AddAsync(new DbCustomDownloader
        {
            CustomDownloaderId = definition.Id,
            Name = definition.Name,
            DefinitionJson = SelectorDownloaderJson.Serialize(definition)
        }, ct);
        await mangaContext.SaveChangesAsync(ct);

        await CustomDownloaderChanges.ApplyAsync(eventPublisher, ct);
        return TypedResults.Ok(definition);
    }
}

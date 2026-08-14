using Common.Services.Events;
using Common.Services.Events.Events;
using Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Services.Manga.Features.Suwayomi;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PostSuwayomiRefreshEndpoint
{
    /// <summary>
    /// Re-read the sources installed on the Suwayomi sidecar
    /// </summary>
    /// <param name="eventPublisher"></param>
    /// <param name="ct"></param>
    /// <returns>The number of Suwayomi-backed download extensions now registered</returns>
    /// <remarks>
    /// Installing and uninstalling already refresh automatically. This is the manual escape hatch for when extensions
    /// were changed outside Tranga, or a service started before the sidecar was ready.
    /// </remarks>
    /// <response code="200">The number of Suwayomi-backed download extensions now registered</response>
    public static async Task<Ok<int>> Handle([FromServices]EventPublisher eventPublisher, CancellationToken ct)
    {
        int count = await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(ct);
        await eventPublisher.PublishAsync(new SuwayomiSourcesChangedEvent(), ct);
        return TypedResults.Ok(count);
    }
}

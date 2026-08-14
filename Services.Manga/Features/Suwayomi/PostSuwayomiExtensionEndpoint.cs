using Common.Services.Events;
using Common.Services.Events.Events;
using Extensions;
using Extensions.Extensions.Suwayomi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Services.Manga.Features.Suwayomi;

/// <summary>
/// Installing, updating and uninstalling a Suwayomi extension. All three change the set of available sources, so each
/// refreshes this service's extension collection and tells the other services to do the same.
/// </summary>
internal abstract class PostSuwayomiExtensionEndpoint
{
    /// <summary>
    /// Install a Suwayomi extension
    /// </summary>
    /// <param name="eventPublisher"></param>
    /// <param name="pkgName">Package name of the extension, as returned by the extension catalogue</param>
    /// <param name="ct"></param>
    /// <response code="200">The extension was installed and its sources are now available</response>
    /// <response code="503">The Suwayomi sidecar is disabled, unreachable, or refused the install</response>
    public static Task<Results<Ok, StatusCodeHttpResult>> Install([FromServices]EventPublisher eventPublisher, [FromRoute]string pkgName, CancellationToken ct) =>
        Apply(() => SuwayomiExtensionManager.InstallAsync(pkgName, ct), eventPublisher, ct);

    /// <summary>
    /// Update a Suwayomi extension
    /// </summary>
    /// <param name="eventPublisher"></param>
    /// <param name="pkgName">Package name of the extension</param>
    /// <param name="ct"></param>
    /// <response code="200">The extension was updated</response>
    /// <response code="503">The Suwayomi sidecar is disabled, unreachable, or refused the update</response>
    public static Task<Results<Ok, StatusCodeHttpResult>> Update([FromServices]EventPublisher eventPublisher, [FromRoute]string pkgName, CancellationToken ct) =>
        Apply(() => SuwayomiExtensionManager.UpdateAsync(pkgName, ct), eventPublisher, ct);

    /// <summary>
    /// Uninstall a Suwayomi extension
    /// </summary>
    /// <param name="eventPublisher"></param>
    /// <param name="pkgName">Package name of the extension</param>
    /// <param name="ct"></param>
    /// <remarks>
    /// Download links pointing at this extension's sources stop resolving until it is installed again. They are not
    /// deleted, so re-installing the extension restores them.
    /// </remarks>
    /// <response code="200">The extension was uninstalled</response>
    /// <response code="503">The Suwayomi sidecar is disabled, unreachable, or refused the uninstall</response>
    public static Task<Results<Ok, StatusCodeHttpResult>> Uninstall([FromServices]EventPublisher eventPublisher, [FromRoute]string pkgName, CancellationToken ct) =>
        Apply(() => SuwayomiExtensionManager.UninstallAsync(pkgName, ct), eventPublisher, ct);

    private static async Task<Results<Ok, StatusCodeHttpResult>> Apply(Func<Task<bool>> action, EventPublisher eventPublisher, CancellationToken ct)
    {
        if (!await action())
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);

        await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(ct);
        await eventPublisher.PublishAsync(new SuwayomiSourcesChangedEvent(), ct);

        return TypedResults.Ok();
    }
}

using Common.Services.Events;
using Common.Services.Events.Events;
using Extensions;

namespace Services.Manga.Features.CustomDownloaders;

/// <summary>What has to happen everywhere once the stored custom sources changed.</summary>
internal static class CustomDownloaderChanges
{
    /// <summary>
    /// Reloads the download extensions of this service and tells the other services to do the same.
    /// <para>
    /// The change is already saved when this runs, so a message bus that is down must not turn it into an error:
    /// the other services load the stored sources again on their next start.
    /// </para>
    /// </summary>
    internal static async Task ApplyAsync(EventPublisher eventPublisher, CancellationToken ct)
    {
        await DownloadExtensionsCollection.RefreshAsync(ct);
        try
        {
            await eventPublisher.PublishAsync(new DownloadExtensionsChangedEvent(), ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // See above.
        }
    }
}

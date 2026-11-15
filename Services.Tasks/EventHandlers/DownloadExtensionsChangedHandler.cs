using Common.Services.Events;
using Common.Services.Events.Events;
using Extensions;
using RabbitMQ.Client;

namespace Services.Tasks.EventHandlers;

/// <summary>
/// Reloads the download extensions after the manga service created, changed or deleted a custom source. Each service
/// process keeps its own <see cref="DownloadExtensionsCollection"/>, so this is what stops the task service from
/// downloading against a stale source list.
/// </summary>
internal sealed class DownloadExtensionsChangedHandler(IChannel channel) : TrangaEventHandler<DownloadExtensionsChangedEvent>(channel)
{
    protected override async Task<bool> HandleMessage(DownloadExtensionsChangedEvent notificationEvent)
    {
        await DownloadExtensionsCollection.RefreshAsync(CancellationToken.None);
        return true;
    }
}

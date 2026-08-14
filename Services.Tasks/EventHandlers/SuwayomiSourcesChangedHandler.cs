using Common.Services.Events;
using Common.Services.Events.Events;
using Extensions;
using RabbitMQ.Client;

namespace Services.Tasks.EventHandlers;

/// <summary>
/// Re-registers the Suwayomi-backed download extensions after the manga service installed or removed an extension.
/// Each service process keeps its own <see cref="DownloadExtensionsCollection"/>, so this is what stops the task
/// service from downloading against a stale source list.
/// </summary>
internal sealed class SuwayomiSourcesChangedHandler(IChannel channel) : TrangaEventHandler<SuwayomiSourcesChangedEvent>(channel)
{
    protected override async Task<bool> HandleMessage(SuwayomiSourcesChangedEvent notificationEvent)
    {
        await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(CancellationToken.None);
        return true;
    }
}

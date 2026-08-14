using Extensions;
using Extensions.Extensions.Suwayomi;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Tasks;

/// <summary>
/// Re-reads the sources installed on the Suwayomi sidecar and re-registers them as download extensions.
/// <para>
/// Installing or uninstalling an extension already publishes a <c>SuwayomiSourcesChangedEvent</c> that refreshes this
/// service immediately. This task is the safety net for the cases that event cannot cover: the sidecar being started
/// after this service, extensions changed outside Tranga, or a missed message.
/// </para>
/// </summary>
internal sealed class SuwayomiSourceRefreshTask() : PeriodicTask(Guid.Parse("0b6f5a44-6ad9-4a7c-9a2f-3c1d8e5b7f22"))
{
    internal override TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        if (!SuwayomiSource.IsAvailable)
        {
            logger.LogDebug("Suwayomi sidecar is disabled, skipping source refresh.");
            return;
        }

        int count = await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(stoppingToken);
        logger.LogDebug("Suwayomi sidecar provides {count} download extensions.", count);
    }

    protected override void RefreshScope(IServiceScope scope)
    {
    }
}

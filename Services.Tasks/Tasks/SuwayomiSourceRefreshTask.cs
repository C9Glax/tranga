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
/// <para>
/// Its first run after start also pulls the extension catalogue from the configured stores — see
/// <see cref="_catalogueFetched"/>.
/// </para>
/// </summary>
internal sealed class SuwayomiSourceRefreshTask() : PeriodicTask(Guid.Parse("0b6f5a44-6ad9-4a7c-9a2f-3c1d8e5b7f22"))
{
    internal override TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// A fresh sidecar has an empty extension table until something asks it to read the configured stores, which left
    /// the sources page blank until the user pressed "Refresh catalogue". Pulling the catalogue once per Tranga start
    /// covers that, and picks up extensions published since last time. It is deliberately not done on every run: it is
    /// a network fetch of ~1400 entries, and nothing about it goes stale within an hour.
    /// </summary>
    private bool _catalogueFetched;

    /// <summary>
    /// How long the first run waits for the sidecar, which is slower to boot than the services that depend on it.
    /// Kept short on purpose: this occupies a task worker while it waits, and the pool can be as small as one.
    /// Overshooting it is not fatal, only slower — the catalogue is then fetched on the next run.
    /// </summary>
    private static readonly TimeSpan SidecarStartupTimeout = TimeSpan.FromSeconds(90);

    private static readonly TimeSpan SidecarPollInterval = TimeSpan.FromSeconds(5);

    protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        if (!_catalogueFetched)
        {
            if (await WaitForSidecar(logger, stoppingToken))
            {
                logger.LogInformation("Fetching Suwayomi extension catalogue...");
                if (await SuwayomiExtensionManager.GetExtensionsAsync(refresh: true, stoppingToken) is { } extensions)
                {
                    _catalogueFetched = true;
                    logger.LogInformation("Suwayomi extension catalogue has {count} extensions.", extensions.Length);
                }
                else
                {
                    logger.LogWarning("Could not fetch the Suwayomi extension catalogue; retrying on the next run.");
                }
            }
            else
            {
                logger.LogWarning("Suwayomi sidecar did not become reachable; retrying on the next run.");
            }
        }

        int count = await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(stoppingToken);
        logger.LogDebug("Suwayomi sidecar provides {count} download extensions.", count);
    }

    /// <summary>
    /// Polls the sidecar until it answers or <see cref="SidecarStartupTimeout"/> passes. Without this the first run
    /// would almost always land while the sidecar is still starting — compose only waits for it to have *started* —
    /// and the catalogue would then not arrive until the next hourly run.
    /// </summary>
    private static async Task<bool> WaitForSidecar(ILogger logger, CancellationToken stoppingToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + SidecarStartupTimeout;
        while (!stoppingToken.IsCancellationRequested)
        {
            if ((await SuwayomiExtensionManager.GetStatusAsync(stoppingToken)).Reachable)
                return true;
            if (DateTimeOffset.UtcNow >= deadline)
                return false;

            logger.LogDebug("Waiting for the Suwayomi sidecar to become reachable...");
            await Task.Delay(SidecarPollInterval, stoppingToken);
        }
        return false;
    }

    protected override void RefreshScope(IServiceScope scope)
    {
    }
}

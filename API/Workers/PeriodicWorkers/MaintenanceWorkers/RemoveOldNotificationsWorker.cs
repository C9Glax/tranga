using System.Diagnostics.CodeAnalysis;
using API.Schema.NotificationsContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers.MaintenanceWorkers;

/// <summary>
/// Removes sent notifications from database
/// </summary>
public class RemoveOldNotificationsWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContexts(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ??  TimeSpan.FromHours(1);

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private NotificationsContext NotificationsContext = null!;

    protected override void SetContexts(IServiceScope serviceScope)
    {
        NotificationsContext = GetContext<NotificationsContext>(serviceScope);
    }
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug("Removing old notifications...");
        int removed = await NotificationsContext.Notifications.Where(n => n.IsSent).ExecuteDeleteAsync(CancellationToken);
        Log.DebugFormat("Removed {0} old notifications...", removed);
        
        if(await NotificationsContext.Sync(CancellationToken, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } e)
            Log.ErrorFormat("Failed to save database changes: {0}", e.exceptionMessage);
        
        return [];
    }

}
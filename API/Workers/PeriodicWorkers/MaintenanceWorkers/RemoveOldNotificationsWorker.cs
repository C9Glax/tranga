using API.Schema.NotificationsContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers.MaintenanceWorkers;

/// <summary>
/// Removes sent notifications from database
/// </summary>
public class RemoveOldNotificationsWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<NotificationsContext>(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ??  TimeSpan.FromHours(1);
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug("Removing old notifications...");
        int removed = await DbContext.Notifications.Where(n => n.IsSent).ExecuteDeleteAsync(CancellationToken);
        Log.Debug($"Removed {removed} old notifications...");
        
        if(await DbContext.Sync(CancellationToken, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } e)
            Log.Error($"Failed to save database changes: {e.exceptionMessage}");
        
        return [];
    }

}
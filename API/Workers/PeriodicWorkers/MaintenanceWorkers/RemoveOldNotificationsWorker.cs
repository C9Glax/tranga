using API.Schema.NotificationsContext;

namespace API.Workers.MaintenanceWorkers;

public class RemoveOldNotificationsWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<NotificationsContext>(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ??  TimeSpan.FromHours(1);
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        IQueryable<Notification> toRemove = DbContext.Notifications.Where(n => n.IsSent || DateTime.UtcNow - n.Date > Interval);
        DbContext.RemoveRange(toRemove);
        
        await DbContext.Sync(CancellationTokenSource.Token);
        return [];
    }

}
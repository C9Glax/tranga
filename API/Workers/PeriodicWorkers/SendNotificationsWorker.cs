using System.Diagnostics.CodeAnalysis;
using API.Schema.NotificationsContext;
using API.Schema.NotificationsContext.NotificationConnectors;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers;

/// <summary>
/// Send notifications to NotificationConnectors
/// </summary>
/// <param name="interval"></param>
/// <param name="dependsOn"></param>
public class SendNotificationsWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContexts(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval??TimeSpan.FromMinutes(1);
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private NotificationsContext NotificationsContext = null!;

    protected override void SetContexts(IServiceScope serviceScope)
    {
        NotificationsContext = GetContext<NotificationsContext>(serviceScope);
    }
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug("Sending notifications...");
        List<NotificationConnector> connectors = await NotificationsContext.NotificationConnectors.ToListAsync(CancellationToken);
        List<Notification> unsentNotifications = await NotificationsContext.Notifications.Where(n => n.IsSent == false).ToListAsync(CancellationToken);
        
        Log.Debug($"Sending {unsentNotifications.Count} notifications to {connectors.Count} connectors...");
        
        unsentNotifications.ForEach(notification =>
        {
            connectors.ForEach(connector =>
            {
                connector.SendNotification(notification.Title, notification.Message);
                NotificationsContext.Entry(notification).Property(n => n.IsSent).CurrentValue = true;
            });
        });
        
        Log.Debug("Notifications sent.");
        
        if(await NotificationsContext.Sync(CancellationToken, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } e)
            Log.Error($"Failed to save database changes: {e.exceptionMessage}");
            
        return [];
    }
}
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
        if (await NotificationsContext.Notifications.Where(n => !n.IsSent).ToListAsync(CancellationToken) is not
            { Count: > 0 } unsentNotifications)
        {
            Log.Debug("No new notifications.");
            return [];
        }
        List<NotificationConnector> connectors = await NotificationsContext.NotificationConnectors.ToListAsync(CancellationToken);
        
        Log.DebugFormat("Sending {0} notifications to {1} connectors...", unsentNotifications.Count, connectors.Count);

        foreach (var groupedNotification in unsentNotifications.GroupBy(n => n.Title, n => n).Select(g => new { Title = g.Key, Notifications = g.ToList() }))
        {
            connectors.ForEach(connector =>
            {
                connector.SendNotification(groupedNotification.Title, string.Join('\n', groupedNotification.Notifications.Select(n => n.Message)));
            });
            groupedNotification.Notifications.ForEach(n => n.IsSent = true);
        }
        
        Log.Debug("Notifications sent.");
        
        if(await NotificationsContext.Sync(CancellationToken, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } e)
            Log.ErrorFormat("Failed to save database changes: {0}", e.exceptionMessage);
            
        return [];
    }
}
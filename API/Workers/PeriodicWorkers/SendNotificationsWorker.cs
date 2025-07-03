using API.Schema.NotificationsContext;
using API.Schema.NotificationsContext.NotificationConnectors;

namespace API.Workers;

public class SendNotificationsWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<NotificationsContext>(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval??TimeSpan.FromMinutes(1);
    protected override BaseWorker[] DoWorkInternal()
    {
        NotificationConnector[] connectors = DbContext.NotificationConnectors.ToArray();
        Notification[] notifications = DbContext.Notifications.Where(n => n.IsSent == false).ToArray();
        
        foreach (Notification notification in notifications)
        {
            foreach (NotificationConnector connector in connectors)
            {
                connector.SendNotification(notification.Title, notification.Message);
                notification.IsSent = true;
            }
        }
        
        DbContext.Sync();
        return [];
    }

}
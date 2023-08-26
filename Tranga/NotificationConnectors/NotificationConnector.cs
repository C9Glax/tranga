namespace Tranga.NotificationConnectors;

public abstract class NotificationConnector : GlobalBase
{
    public NotificationManagerType notificationManagerType;

    protected NotificationConnector(GlobalBase clone, NotificationManagerType notificationManagerType) : base(clone)
    {
        this.notificationManagerType = notificationManagerType;
    }
    
    public enum NotificationManagerType : byte { Gotify = 0, LunaSea = 1 }
    
    public abstract void SendNotification(string title, string notificationText);
}
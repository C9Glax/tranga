namespace Tranga.NotificationConnectors;

public abstract class NotificationConnector : TBaseObject
{
    public NotificationManagerType notificationManagerType;

    protected NotificationConnector(NotificationManagerType notificationManagerType, TBaseObject clone) : base(clone)
    {
        this.notificationManagerType = notificationManagerType;
    }
    
    public enum NotificationManagerType : byte { Gotify = 0, LunaSea = 1 }
    
    public abstract void SendNotification(string title, string notificationText);
}
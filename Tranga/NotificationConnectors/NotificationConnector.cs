﻿namespace Tranga.NotificationConnectors;

public abstract class NotificationConnector : GlobalBase
{
    public readonly NotificationConnectorType notificationConnectorType;

    protected NotificationConnector(GlobalBase clone, NotificationConnectorType notificationConnectorType) : base(clone)
    {
        Log($"Creating notificationConnector {Enum.GetName(notificationConnectorType)}");
        this.notificationConnectorType = notificationConnectorType;
    }
    
    public enum NotificationConnectorType : byte { Gotify = 0, LunaSea = 1, Ntfy = 2 }
    
    public abstract void SendNotification(string title, string notificationText);
}
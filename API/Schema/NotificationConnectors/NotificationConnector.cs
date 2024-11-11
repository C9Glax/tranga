using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.NotificationConnectors;

[PrimaryKey("NotificationConnectorId")]
public abstract class NotificationConnector(string notificationConnectorId, Tranga.NotificationConnectors.NotificationConnector.NotificationConnectorType notificationConnectorType)
{
    [MaxLength(64)]
    public string NotificationConnectorId { get; } = notificationConnectorId;

    public Tranga.NotificationConnectors.NotificationConnector.NotificationConnectorType NotificationConnectorType
    {
        get;
        init;
    } = notificationConnectorType;
}
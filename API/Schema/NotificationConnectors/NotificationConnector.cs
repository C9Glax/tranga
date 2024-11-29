using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.NotificationConnectors;

[PrimaryKey("NotificationConnectorId")]
public abstract class NotificationConnector(string notificationConnectorId, NotificationConnectorType notificationConnectorType)
{
    [MaxLength(64)]
    public string NotificationConnectorId { get; } = notificationConnectorId;

    public NotificationConnectorType NotificationConnectorType { get; init; } = notificationConnectorType;
}
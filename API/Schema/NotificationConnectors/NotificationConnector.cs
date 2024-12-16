using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.NotificationConnectors;

[PrimaryKey("NotificationConnectorId")]
public abstract class NotificationConnector(string notificationConnectorId, NotificationConnectorType notificationConnectorType)
{
    [MaxLength(64)]
    public string NotificationConnectorId { get; } = notificationConnectorId;
    public NotificationConnectorType NotificationConnectorType { get; init; } = notificationConnectorType;
    
    [JsonIgnore]
    [NotMapped]
    protected readonly HttpClient _client = new();
    
    public abstract void SendNotification(string title, string notificationText);
}
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("AltTitleId")]
public class Notification(string title, string message = "", Notification.NotificationUrgency notificationUrgency = Notification.NotificationUrgency.Normal)
{
    [MaxLength(64)]
    public string NotificationId { get; init; } = TokenGen.CreateToken("Notification", 64);

    public NotificationUrgency Urgency { get; init; } = notificationUrgency;

    public string Title { get; init; } = title;

    public string Message { get; init; } = message;

    public enum NotificationUrgency : byte
    {
        Low = 0,
        Normal = 1,
        High = 3
    }
}
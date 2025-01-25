using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("NotificationId")]
public class Notification(string title, string message = "", NotificationUrgency urgency = NotificationUrgency.Normal, DateTime? date = null)
{
    [MaxLength(64)]
    public string NotificationId { get; init; } = TokenGen.CreateToken("Notification", "");

    public NotificationUrgency Urgency { get; init; } = urgency;

    public string Title { get; init; } = title;

    public string Message { get; init; } = message;
    
    public DateTime Date { get; init; } = date ?? DateTime.UtcNow;
    
    public Notification() : this("") { }
}
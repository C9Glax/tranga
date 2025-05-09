using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("NotificationId")]
public class Notification
{
    [StringLength(64)]
    [Required]
    public string NotificationId { get; init; }

    [Required]
    public NotificationUrgency Urgency { get; init; }

    [StringLength(128)]
    [Required]
    public string Title { get; init; }

    [StringLength(512)]
    [Required]
    public string Message { get; init; }
    
    [Required]
    public DateTime Date { get; init; }

    public Notification(string title, string message = "", NotificationUrgency urgency = NotificationUrgency.Normal, DateTime? date = null)
    {
        this.NotificationId = TokenGen.CreateToken("Notification");
        this.Title = title;
        this.Message = message;
        this.Urgency = urgency;
        this.Date = date ?? DateTime.UtcNow;
    }

    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    public Notification(string notificationId, string title, string message, NotificationUrgency urgency, DateTime date)
    {
        this.NotificationId = notificationId;
        this.Title = title;
        this.Message = message;
        this.Urgency = urgency;
        this.Date = date;
    }

    public override string ToString()
    {
        return $"{NotificationId} {Urgency} {Title}";
    }
}